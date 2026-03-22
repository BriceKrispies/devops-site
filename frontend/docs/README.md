# DevOps Site — Frontend Architecture

## What This Is

A micro frontend framework for a DevOps dashboard. It renders static HTML immediately, enhances it with vanilla TypeScript, and fills in live data through a centralized pub/sub data layer. The data layer starts with mocked adapters and swaps to real backend connectors without changing any UI code.

Stack: **Vite + vanilla TypeScript + plain HTML + layered CSS with design tokens.**

No frameworks. No virtual DOM. No build-time component model. The browser's native platform is the framework.

## Problems It Solves

1. **Slow first paint.** Most dashboards show a blank screen while JS bundles load and API calls resolve. This system renders a complete HTML shell instantly — every page is usable (or at least visible) before any async work begins.

2. **Tight coupling between UI and backend.** Typical frontends break when the backend isn't ready or changes shape. This system places an adapter boundary between UI and data — mocked adapters let the frontend run fully standalone, and real connectors slot in behind the same interface.

3. **Unpredictable loading behavior.** Dashboards often freeze, flash, or jump as data arrives. This system requires every async region to own its own loading/error/empty state, so the page stays stable and predictable.

4. **Scattered state.** This system uses a single centralized pub/sub store for all data flow. Components subscribe to topics. Adapters publish to topics. There is one path for data to move through the system.

## Architectural Goals

- HTML renders first, always.
- TypeScript enhances rendered HTML — it does not create it.
- All async data flows through a centralized pub/sub store.
- Every async region manages its own placeholder/loading/error/resolved states.
- The UI never blocks on data. Data arrives, regions update.
- Mocked data and real backend data share identical interfaces.
- CSS uses a minimal tokenized design system with layered specificity.
- The system stays small, auditable, and debuggable.

## Non-Goals

- **Not a general-purpose framework.** This is purpose-built for this DevOps site.
- **Not server-side rendered.** HTML is static and shipped as-is. There is no server rendering step.
- **Not component-library scale.** Components are lightweight HTML/TS/CSS units, not reusable widget kits.
- **Not abstracting the browser.** We use the DOM directly. No virtual DOM, no diffing, no reconciliation.
- **Not optimizing for bundle size at the expense of clarity.** Code should be readable first.

## Core Priorities (Ordered)

1. **Immediate HTML render** — the user sees a complete page shell on every navigation, before any JS executes.
2. **Non-blocking UI** — no operation freezes the interface. Async work happens in the background; the UI stays responsive.
3. **Predictable loading states** — every async region shows a placeholder, then resolves to content, error, or empty. No flashing, no layout shift.
4. **Centralized observable data flow** — all state moves through the pub/sub store. No side-channel data passing.
5. **Adapter-swappable data sources** — mocked and real connectors are interchangeable behind a shared contract.
6. **Minimal, maintainable styling** — design tokens, layered CSS, no utility-class sprawl.
7. **Simplicity** — every abstraction must earn its place. Prefer fewer files, fewer indirections, fewer concepts.
