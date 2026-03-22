# Project Structure

```
devops-site/
├── docs/                        # Architecture docs (this folder)
├── public/                      # Static assets served as-is by Vite
│   └── favicon.ico
├── scripts/
│   └── check-boundaries.sh     # Layer dependency rule validator
├── index.html                   # Dashboard / landing page (Vite root entry)
├── src/
│   ├── pages/                   # Additional HTML pages (one per route)
│   │   └── trace.html           # Trace / Timeline Explorer view
│   │
│   ├── platform/                # Scheduler & performance primitives
│   │   └── scheduler.ts         # defer, scheduleIdle, chunked
│   │
│   ├── state/                   # Pub/sub store and topic definitions
│   │   ├── store.ts             # Store implementation
│   │   ├── store.types.ts       # Store interface
│   │   └── topics.ts            # Topic constants and payload types
│   │
│   ├── adapters/                # Data adapters (mock and real)
│   │   ├── adapter.interface.ts # DataAdapter interface
│   │   ├── registry.ts          # Adapter registration and lifecycle
│   │   ├── mock/                # Mock adapters (default)
│   │   │   ├── service-health.ts
│   │   │   ├── deployments.ts
│   │   │   ├── jobs.ts
│   │   │   └── trace.ts
│   │   └── real/                # Real backend connectors (added later)
│   │
│   ├── effects/                 # Async orchestration and transforms
│   │   └── trace-search.ts      # Query → filter → results pipeline
│   │
│   ├── ui/                      # DOM rendering, event binding, dispatch
│   │   ├── dom.ts               # DOM query helpers
│   │   ├── entries/             # Composition roots (one per page)
│   │   │   ├── index.ts         # Dashboard wiring
│   │   │   └── trace.ts         # Trace page wiring
│   │   ├── regions/             # Async region binding logic
│   │   │   ├── region.ts        # Region binding, state machine
│   │   │   └── region.types.ts  # Region interfaces
│   │   └── renderers/           # Pure HTML string generators
│   │       ├── service-health.ts
│   │       ├── deployments.ts
│   │       ├── jobs.ts
│   │       └── trace.ts
│   │
│   ├── types/                   # Shared domain model types
│   │   └── models.ts            # Service, Deployment, Job, TraceEvent
│   │
│   └── styles/                  # All CSS
│       ├── main.css             # Entry point, imports all layers
│       ├── tokens.css           # Design token definitions
│       ├── reset.css            # Minimal CSS reset
│       ├── layout.css           # Page structure styles
│       ├── components.css       # Shared component styles
│       └── utilities.css        # Utility overrides
│
├── vite.config.ts               # Vite configuration
├── tsconfig.json                # TypeScript config
├── package.json
└── CLAUDE.md                    # Instructions for AI agents working in this repo
```

## Layer Purposes

See `docs/architecture-layers.md` for the full dependency graph and invariant.

### `src/platform/`
Scheduling primitives — `defer`, `scheduleIdle`, `chunked`. No application logic. These are the only approved mechanisms for deferring work off the UI thread.

### `src/state/`
The pub/sub store and topic definitions. Synchronous, minimal, no transforms. The store should stay under 100 lines.

### `src/adapters/`
Data source modules. `mock/` contains fake data adapters used during development. `real/` will contain actual backend connectors. Both implement the `DataAdapter` interface (`start/stop`). Adapters publish data into the store — they do not touch the DOM.

### `src/effects/`
Async orchestration and expensive transforms. Effects subscribe to store topics, perform computation (using `platform/scheduler` primitives to avoid blocking), and publish results back to the store.

### `src/ui/`
Everything that touches the DOM. Entry points (composition roots), region binding, and renderers.

- **`ui/entries/`** — One per page. The only place where all layers are wired together. Creates store, registers adapters, wires effects, binds UI.
- **`ui/regions/`** — Generic region binding: subscribes a DOM element to a store topic, manages loading/resolved/error/empty states.
- **`ui/renderers/`** — Pure functions: `(data: T[]) => string`. No side effects, no store access, no adapter imports.

### `src/types/`
Shared domain models used across all layers. These are the data contracts between adapters, effects, and renderers.

## Conventions

- **One entry module per page.** A page's TS entry is the only file that imports from all layers.
- **One adapter per topic.** Each data topic has exactly one adapter responsible for publishing to it.
- **Renderers are pure functions.** `(data: T[]) => string`. No side effects.
- **No circular imports.** The dependency graph flows downward through layers.
- **Expensive work goes through effects.** Filtering, sorting, correlation — never in renderers or entries.
- **Run `npm run check:boundaries`** to validate layer dependency rules.
