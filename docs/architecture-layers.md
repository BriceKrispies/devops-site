# Architecture: Layer Boundaries & Non-Blocking UI Invariant

## Core Invariant

**No application code may block the UI thread.**

Every path from user action to screen update must be structurally reviewable:
rendering is synchronous and cheap, all expensive work is deferred through
the scheduler, and layer boundaries make violations obvious in code review.

## Layers

```
src/
  platform/     Scheduler & performance primitives (no deps)
  state/        Store, topics, pure state (deps: types)
  adapters/     Data access — mock or real (deps: state, types)
  effects/      Async orchestration, search, transforms (deps: state, platform, types, adapters)
  ui/           DOM rendering, event binding, dispatch (deps: state, types)
    entries/    Composition roots — wire all layers (deps: all)
    regions/    Region binding (subscribe → render)
    renderers/  Pure HTML string generators
  types/        Shared domain models (no layer deps)
```

## Dependency Rules

| Layer             | May import from                  | Must NOT import from        |
|-------------------|----------------------------------|-----------------------------|
| `platform/`       | `types/`                         | ui, state, effects, adapters|
| `state/`          | `types/`                         | ui, effects, adapters, platform |
| `adapters/`       | `state/`, `types/`               | ui, effects, platform       |
| `effects/`        | `state/`, `platform/`, `types/`, `adapters/` | ui              |
| `ui/` (non-entry) | `state/`, `types/`               | adapters, effects, platform |
| `ui/entries/`     | all layers                       | —                           |

Run `bash scripts/check-boundaries.sh` to verify. Integrate into CI.

## Why This Structure

### Renderers stay cheap
Renderers (`ui/renderers/`) are pure functions: `data → HTML string`. They have
no side effects, no async, no imports from adapters or effects. If rendering
becomes expensive, the bottleneck is visible and isolated.

### Expensive work goes through effects
Any computation that scales with data size — filtering, sorting, correlation,
normalization — lives in `effects/`. Effects use `platform/scheduler` primitives
(`defer`, `chunked`, `scheduleIdle`) to yield to the browser between steps.

Example: trace search. The adapter publishes raw data to `TRACE_DATA`. The
effect (`effects/trace-search.ts`) subscribes to `TRACE_QUERY`, reads raw data
from the store, filters via `defer()`, and publishes results to `TRACE_RESULTS`.
The UI never touches the search logic.

### Adapters are data boundaries
Adapters implement `start(store)/stop()`. They publish data into the store and
nothing else. Real adapters will fetch from APIs; mock adapters provide static
data with simulated latency. Adapters must not import DOM code or UI logic.

### State is synchronous and minimal
The store (`state/store.ts`) is a synchronous pub/sub cache. Publish and
subscribe are O(subscribers) — no transforms, no middleware, no async. Topic
payloads use `TopicPayload<T>` wrappers (`status: "ok"|"error"`, `data?`, `error?`).

### Platform is the scheduling seam
`platform/scheduler.ts` exports `defer`, `scheduleIdle`, `cancelIdle`, and
`chunked`. These are the only approved mechanisms for deferring work. This makes
it easy to audit: grep for `setTimeout`/`requestAnimationFrame` outside
`platform/` — any hit is a candidate for refactoring.

## Entry Points Are Composition Roots

`ui/entries/*.ts` files are the only place where all layers meet. They:

1. Create the store
2. Register adapters (data sources)
3. Wire effects (async orchestration)
4. Bind UI regions or event handlers
5. Set up cleanup on unload

No business logic lives in entries. They are pure wiring.
