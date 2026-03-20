# System Map

Compact cross-reference of the entire frontend architecture.

---

## Rendering Model

Static HTML renders first. TypeScript enhances it. Async regions hydrate independently.

| Phase | What happens | Depends on JS |
|-------|-------------|---------------|
| HTML shell paint | Browser renders full page layout + skeletons | No |
| Enhancement | Store created, adapters started, regions bound | Yes |
| Async hydration | Regions transition from loading → resolved | Yes |

**Key files:** `index.html`, `src/pages/*.html`, `src/ui/entries/*.ts`, `src/ui/regions/region.ts`
**Docs:** [rendering.md](./rendering.md)

---

## State / Data Flow

Single pub/sub store. Adapters publish. Regions subscribe. One direction.

```
Adapter → store.publish(topic, data) → store notifies subscribers → region updates DOM
```

- Store caches last value per topic (late subscribers get current state immediately)
- Dispatch is synchronous
- No side-channel data passing

**Key files:** `src/state/store.ts`, `src/state/topics.ts`
**Docs:** [data-flow.md](./data-flow.md)

---

## Styling System

Layered CSS with design tokens. Five layers in specificity order:

```
tokens → reset → layout → components → utilities
```

All visual values (color, spacing, radius, typography, shadow, motion) come from CSS custom properties defined in the `tokens` layer. Components consume tokens via `var()`.

**Key files:** `src/styles/tokens.css`, `src/styles/main.css`
**Docs:** [styling.md](./styling.md)

---

## Async Loading Model

Every `data-region` element has four states:

| State | Trigger | Visual |
|-------|---------|--------|
| `loading` | Initial (from HTML) | Skeleton placeholder |
| `resolved` | Adapter publishes data | Rendered content |
| `error` | Adapter publishes error | Error message + retry |
| `empty` | Adapter publishes empty set | Empty state message |

Regions are independent. One slow region does not block others. State is tracked via `data-state` attribute on the DOM element.

**Key files:** `src/ui/regions/region.ts`, `src/pages/*.html` (skeleton markup)
**Docs:** [rendering.md](./rendering.md)

---

## Adapter Boundary

```
┌──────────────┐     ┌───────┐     ┌──────────────┐
│ Mock Adapter  │────►│       │     │              │
└──────────────┘     │ Store │────►│  UI Regions   │
┌──────────────┐     │       │     │              │
│ Real Adapter  │────►│       │     │              │
└──────────────┘     └───────┘     └──────────────┘
```

Both adapter types implement `DataAdapter` (`start(store)` / `stop()`). The store and UI do not know which type is active. Swapping requires changing one import in the entry module.

**Key files:** `src/adapters/adapter.interface.ts`, `src/adapters/mock/*`, `src/adapters/real/*`
**Docs:** [data-flow.md](./data-flow.md)

---

## Core Invariants (Summary)

| # | Rule |
|---|------|
| 1 | HTML renders immediately, no JS dependency |
| 2 | No page needs async data for first paint |
| 3 | Every async region owns its own loading state |
| 4 | UI updates are incremental (no full re-renders) |
| 5 | Mock and real adapters share the same interface |
| 6 | All data flows through the centralized store |
| 7 | Store is the single source of truth |
| 8 | State flow is centralized and observable |
| 9 | Main thread must not be blocked |
| 10 | CSS tokens are the single source of design values |
| 11 | No framework dependencies |
| 12 | Adapters are the only external boundary |

**Docs:** [invariants.md](./invariants.md)

---

## File Map

| Area | Location | Purpose |
|------|----------|---------|
| Dashboard | `index.html` | Root page (Vite entry) |
| Pages | `src/pages/` | Additional HTML pages |
| Platform | `src/platform/` | Scheduler & performance primitives |
| State | `src/state/` | Pub/sub store + topic definitions |
| Adapters | `src/adapters/` | Data sources (mock + real) |
| Effects | `src/effects/` | Async orchestration + transforms |
| Entry modules | `src/ui/entries/` | Page composition roots |
| Regions | `src/ui/regions/` | Region binding + state machine |
| Renderers | `src/ui/renderers/` | Data → HTML string functions |
| Styles | `src/styles/` | Layered CSS + tokens |
| Types | `src/types/` | Shared domain model types |

**Docs:** [project-structure.md](./project-structure.md)
