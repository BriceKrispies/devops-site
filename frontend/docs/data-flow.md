# Data Flow

## Overview

All data in the frontend moves through a single centralized pub/sub store. This document describes the exact mechanics.

```
Adapter ──publish(topic, data)──► Store ──notify──► Subscriber A (region)
                                       ──notify──► Subscriber B (region)
                                       ──notify──► Subscriber C (logger)
```

There is one store instance per page. All adapters publish to it. All regions subscribe to it.

## Core Concepts

### Topic

A topic is a string key that identifies a data channel. Examples:

- `"pipelines"` — list of CI/CD pipelines
- `"deployments.recent"` — recent deployment history
- `"cluster.health"` — current cluster health status
- `"alerts.active"` — active alert list

Topics are flat strings. Use dot notation for namespacing, but the store treats them as opaque keys — there is no hierarchical dispatch.

### Store

The store is an object with this interface:

```ts
interface Store {
  publish<T>(topic: string, data: T): void;
  subscribe<T>(topic: string, callback: (data: T) => void): Unsubscribe;
  get<T>(topic: string): T | undefined;
}

type Unsubscribe = () => void;
```

**Behavior:**

- `publish(topic, data)` — stores the value and synchronously calls every subscriber for that topic with the new value.
- `subscribe(topic, callback)` — registers a callback. If the topic already has a value (last-value cache), the callback is called immediately with that value. Returns an unsubscribe function.
- `get(topic)` — returns the latest value for the topic, or `undefined` if nothing has been published yet.

### Publisher (Adapter)

An adapter calls `store.publish(topic, data)` whenever it has new data. This is the only way data enters the store.

Mock adapters publish on timers or immediately. Real adapters publish when API responses arrive or WebSocket messages come in.

### Subscriber (Region or utility)

A region calls `store.subscribe(topic, callback)` to receive data. The callback handles the state transition:

```ts
store.subscribe("pipelines", (data) => {
  if (data.length === 0) {
    region.setState("empty");
  } else {
    region.setState("resolved", renderPipelines(data));
  }
});
```

Subscribers can also be non-UI: loggers, analytics, derived-data processors.

## Data Flow Sequence

### Page load

1. HTML renders (shell + placeholder regions).
2. Entry TS module runs.
3. Store is created.
4. Adapters are registered and started — each receives the store reference.
5. Regions subscribe to their topics.
6. Adapters begin publishing data (immediately for mocks, after API calls for real).
7. Subscribers fire — regions transition from `loading` to their resolved state.

### Runtime update

1. An adapter publishes a new value to a topic.
2. The store updates its internal cache for that topic.
3. The store synchronously iterates all subscribers for that topic, calling each with the new value.
4. Each subscriber (region) updates its DOM.

### Error handling

Adapters are responsible for catching their own errors. When an adapter encounters an error, it publishes an error signal to the topic:

```ts
interface TopicPayload<T> {
  status: "ok" | "error";
  data?: T;
  error?: string;
}
```

The region subscriber checks `status` and transitions to the `error` state when appropriate.

## Mocked Data Adapters

A mock adapter is a module that publishes realistic fake data. Example:

```ts
// src/adapters/mock/pipelines.ts
import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../store/store";

const MOCK_PIPELINES = [
  { id: "1", name: "api-build", status: "passing", duration: 94 },
  { id: "2", name: "web-deploy", status: "failing", duration: 212 },
];

export const mockPipelinesAdapter: DataAdapter = {
  start(store: Store) {
    // Simulate async delay
    setTimeout(() => {
      store.publish("pipelines", { status: "ok", data: MOCK_PIPELINES });
    }, 600);
  },
  stop() {},
};
```

Mock adapters are the default during development. They let the entire frontend run without any backend.

## Real Backend Connectors

A real adapter replaces a mock adapter. It implements the same `DataAdapter` interface but fetches from an actual API:

```ts
// src/adapters/real/pipelines.ts
import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../store/store";

export const realPipelinesAdapter: DataAdapter = {
  private controller: AbortController | null = null;

  start(store: Store) {
    this.controller = new AbortController();
    fetch("/api/pipelines", { signal: this.controller.signal })
      .then((res) => res.json())
      .then((data) => store.publish("pipelines", { status: "ok", data }))
      .catch((err) => {
        if (err.name !== "AbortError") {
          store.publish("pipelines", { status: "error", error: err.message });
        }
      });
  },

  stop() {
    this.controller?.abort();
  },
};
```

### Swapping Adapters

Adapter registration happens in one place per page (the entry module). Swapping mock for real:

```ts
// Before (mock)
import { mockPipelinesAdapter } from "./adapters/mock/pipelines";
registerAdapter(mockPipelinesAdapter);

// After (real)
import { realPipelinesAdapter } from "./adapters/real/pipelines";
registerAdapter(realPipelinesAdapter);
```

Nothing else changes. The store, regions, and all UI code remain untouched.

## Rules

1. **Data only enters the store through adapters.** No region, utility, or module may call `store.publish()` directly.
2. **Regions only read from the store.** They subscribe; they do not publish.
3. **Each topic has one authoritative adapter.** Two adapters must not publish to the same topic (this avoids conflicting state).
4. **Subscriptions must be cleaned up.** When a region is removed or a page is torn down, all `Unsubscribe` functions must be called.
5. **The store dispatch is synchronous.** Subscribers must not assume async delivery. When `publish()` is called, all subscribers have already been notified by the time it returns.
