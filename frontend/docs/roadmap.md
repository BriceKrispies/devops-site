# Roadmap

A phased evolution from static HTML shell to fully backend-connected DevOps dashboard.

---

## Phase 1: Static Shell

**Goal:** Every page renders a complete HTML layout with placeholder content. No TypeScript, no data, no interactivity.

**Deliverables:**
- HTML pages for all primary views (dashboard, pipelines, deployments, etc.)
- Full page chrome (nav, sidebar, header, footer)
- Skeleton placeholders in every region that will later show live data
- CSS token system, reset, layout styles, and skeleton component styles
- Vite configured for multi-page mode

**Exit criteria:** Every page loads instantly and looks like a complete (if static) application. JS can be disabled with no visual degradation.

---

## Phase 2: Centralized Store + Mock Data

**Goal:** The pub/sub store exists and mock adapters publish data to it. Regions subscribe and render.

**Deliverables:**
- Store implementation (`publish`, `subscribe`, `get`)
- Region binding system (state machine: loading → resolved/error/empty)
- Mock adapters for every topic
- Renderers for every region
- Entry modules that wire it all together

**Exit criteria:** The app runs fully standalone with no backend. Every region transitions from skeleton to rendered content. Error and empty states are exercised with mock data variants.

---

## Phase 3: Async Regions + Robust Loading

**Goal:** Loading, error, and empty states are polished. The UI handles slow and failing data gracefully.

**Deliverables:**
- Skeleton animations tuned
- Error state UI with retry capability
- Empty state UI with contextual messages
- Simulated slow/failing adapters for testing edge cases
- Region state CSS transitions (fade-in on resolve)

**Exit criteria:** Artificially slow any adapter to 5 seconds — the page remains responsive, the slow region shows its skeleton, and all other regions resolve independently.

---

## Phase 4: Connector Abstraction

**Goal:** The adapter interface is battle-tested and ready for real backends. The swap path is proven.

**Deliverables:**
- Adapter interface reviewed and finalized
- At least one topic has both a mock and real adapter, proving the swap
- Adapter registration supports environment-based selection (dev → mock, prod → real)
- Topic payload types are strict and shared between mock and real adapters

**Exit criteria:** Swapping from mock to real adapter for one topic requires changing exactly one import line in the entry module. Nothing else changes.

---

## Phase 5: Real Backend Integration

**Goal:** Real adapters replace mock adapters, connected to actual APIs.

**Deliverables:**
- Real adapters for all topics
- Proper error handling (network failures, auth errors, malformed responses)
- Abort/cleanup on `stop()`
- Environment configuration for API base URLs

**Exit criteria:** The app runs against real backends. Mock adapters remain available for development and testing.

---

## Phase 6: Richer State and Event Flows

**Goal:** The pub/sub system handles more complex interactions — user-triggered actions, derived state, refresh cycles.

**Deliverables:**
- User action events (trigger a deployment, retry a pipeline) flow through the store
- Derived topics (computed from other topics)
- Polling adapters that refresh data on intervals
- WebSocket adapters for real-time updates where appropriate
- Optimistic UI updates for user actions

**Exit criteria:** The app supports full read/write interaction patterns. User actions publish through the store, adapters handle side effects, and the UI reflects changes incrementally.

---

## What This Roadmap Does Not Cover

- Authentication and authorization (separate concern, likely handled by the backend/proxy)
- Deployment infrastructure (CI/CD for the frontend itself)
- Multi-tenant or multi-cluster support (future scope)
- Mobile-specific responsive design (the dashboard targets desktop/laptop screens)
