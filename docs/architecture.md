# Architecture

## Layers

The frontend has four layers. Data flows downward through them. UI updates flow upward.

```
┌─────────────────────────────────┐
│  HTML Shell (static, immediate) │  ← always renders first
├─────────────────────────────────┤
│  Async Regions (TS-managed)     │  ← own their loading/resolved/error state
├─────────────────────────────────┤
│  Pub/Sub Store (centralized)    │  ← single source of truth for all data
├─────────────────────────────────┤
│  Data Adapters (mock or real)   │  ← pluggable, behind shared interfaces
└─────────────────────────────────┘
```

### Layer 1: HTML Shell

Every page is a static `.html` file that renders a complete layout immediately. This includes:

- Page chrome (nav, sidebar, header, footer)
- Section headings and structural containers
- Placeholder regions for async content (marked with `data-region` attributes)

The HTML shell loads with zero JS dependencies. A user on a slow connection sees the full page structure before any script executes.

### Layer 2: Async Regions

An async region is a DOM element (identified by `data-region="<name>"`) that will be populated with live data. Each region has exactly four visual states:

| State       | What the user sees                     |
|-------------|----------------------------------------|
| `loading`   | Skeleton placeholder or spinner        |
| `resolved`  | Actual content                         |
| `error`     | Error message with optional retry      |
| `empty`     | Intentional empty state ("no items")   |

TypeScript manages these transitions. The HTML shell starts every region in the `loading` state by including placeholder markup inline. When data arrives (or fails), TS swaps the region content.

### Layer 3: Pub/Sub Store

All data flows through a single centralized store that implements publish/subscribe semantics. See [data-flow.md](./data-flow.md) for full details.

- **Adapters** publish data to named topics.
- **Regions** subscribe to topics and re-render when data arrives.
- The store is synchronous in its dispatch — when a value is published, all subscribers are notified in the same tick.
- The store holds the latest value for each topic (last-value cache), so late subscribers immediately receive current state.

### Layer 4: Data Adapters

An adapter is a module that knows how to produce data for one or more topics. Every adapter implements the same interface:

```ts
interface DataAdapter {
  start(store: Store): void;
  stop(): void;
}
```

- `start()` is called once. The adapter begins publishing to the store.
- `stop()` tears down any connections, intervals, or listeners.

Mock adapters publish hardcoded or randomized data. Real adapters will call APIs, open WebSockets, or poll endpoints. The store and the UI do not know or care which kind of adapter is active.

## HTML-First Rendering

The rendering model is:

1. Browser loads `.html` — full page structure is immediately visible.
2. Vite loads the page's entry `.ts` module.
3. The TS module initializes the store, registers adapters, and binds async regions.
4. Adapters begin publishing data.
5. Regions transition from `loading` to `resolved`/`error`/`empty` as data arrives.

Steps 1 happens before steps 2–5. The user always sees the page shell first.

## How TypeScript Enhances HTML

TypeScript does **not** create DOM structure. It operates on DOM that already exists in the HTML file. Its responsibilities:

- Query `data-region` elements and bind them to store topics.
- Swap region content when topic data changes.
- Attach event listeners for interactive elements.
- Manage region state transitions (loading → resolved, etc.).

If you disable JavaScript entirely, the page still renders its full shell with loading placeholders visible. That is the baseline.

## How Non-Blocking Behavior Is Preserved

- **No synchronous data fetching at page load.** Adapters publish asynchronously; the store dispatches synchronously but only after data arrives.
- **No render-blocking scripts.** Entry scripts use `type="module"` (deferred by default) or are placed at the end of `<body>`.
- **No long tasks in the main thread.** If an adapter needs to do heavy processing, it must yield (via `requestIdleCallback`, `setTimeout`, or Web Workers). The pub/sub dispatch itself is lightweight — just iterating a subscriber list.
- **Regions are independent.** One slow region does not block another. Each region subscribes to its own topic(s) and updates independently.

## Boundary Summary

| Boundary                  | Left side              | Right side           | Contract                  |
|---------------------------|------------------------|----------------------|---------------------------|
| HTML ↔ TypeScript         | Static markup          | Enhancement scripts  | `data-region` attributes  |
| TypeScript ↔ Store        | Region bindings        | Pub/sub dispatch     | `subscribe(topic, fn)`    |
| Store ↔ Adapters          | Topic publish/receive  | Data production      | `DataAdapter` interface   |
| Mock adapter ↔ Real adapter | Hardcoded data       | API/WebSocket data   | Same `DataAdapter` interface |
