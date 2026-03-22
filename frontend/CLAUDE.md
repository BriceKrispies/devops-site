# CLAUDE.md — Agent Instructions

Read `docs/` before making changes. The docs are the source of truth for this project's architecture.

## Quick reference

- **Stack:** Vite + vanilla TypeScript + plain HTML + layered CSS
- **Entry points:** `src/ui/entries/index.ts` (dashboard), `src/ui/entries/trace.ts` (trace), plus area landings (queues, databases, logs, operations, admin)
- **Layers:** `platform/` → `state/` → `adapters/` → `effects/` → `ui/` (see `docs/architecture-layers.md`)
- **Store:** `src/state/store.ts` — pub/sub with `publish`, `subscribe`, `get`
- **Regions:** `src/ui/regions/region.ts` — binds `data-region` elements to store topics
- **Adapters:** `src/adapters/mock/` — mock data producers; `src/adapters/real/` — future real connectors
- **Renderers:** `src/ui/renderers/` — pure functions: `(data: T[]) => string`
- **Effects:** `src/effects/` — async orchestration (search, transforms)
- **Capabilities:** `src/capabilities/` — capability shell model and catalog (see below)
- **Types:** `src/state/topics.ts` and `src/types/models.ts`
- **Styles:** `src/styles/main.css` imports layered CSS (tokens → reset → layout → components → utilities)

## Rules

1. HTML must render immediately without JS. Every `data-region` starts with skeleton placeholders in the static HTML.
2. All data flows through the centralized store. No direct fetch calls outside `src/adapters/`.
3. Mock and real adapters implement the same `DataAdapter` interface.
4. CSS uses tokens from `src/styles/tokens.css`. No hardcoded colors or spacing in component/layout CSS.
5. No UI frameworks. No React, Vue, Svelte, Lit, Angular.
6. Respect layer boundaries — run `npm run check:boundaries` to verify.
7. Expensive work (filter, sort, correlate) goes in `effects/`, not `ui/` or `state/`.

## Capability Shell Model

The frontend mirrors the backend's capability classification. Every operational feature (implemented or planned) is registered in `src/capabilities/catalog.ts`.

### Key types (`src/capabilities/types.ts`)

- `CapabilityShell` — id, name, area, description, status, risk, route, permissions
- `CapabilityStatus` — `planned | stub | ready | disabled`
- `RiskLevel` — `low | medium | high | critical`
- `FeatureArea` — `overview | investigate | queues | databases | logs | operations | admin`
- `FeatureAreaMeta` — area id, name, description, nav section, route

### Catalog queries (`src/capabilities/catalog.ts`)

- `getAll()` — all registered capabilities
- `getByArea(area)` — filter by feature area
- `getByStatus(status)` — filter by status
- `getById(id)` — single lookup
- `getAreaMeta(area)` — area metadata
- `AREA_META` — ordered list of all area metadata

### Status-aware rendering

Cards render differently based on `CapabilityStatus`:
- **ready** — clickable link, green left border
- **stub** — visible but non-clickable, yellow left border, "Coming Soon"
- **planned** — dimmed, gray left border, "Planned"
- **disabled** — dimmed, red left border, "Disabled"

High/critical risk capabilities show colored dot indicators on card names.

### Area landing pages

Each area (queues, databases, logs, operations, admin) has:
- HTML page: `src/pages/<area>.html`
- Entry point: `src/ui/entries/<area>.ts`
- Shared bootstrap: `src/ui/entries/area-landing.ts`

Area landings render an area header + grid of capability cards from the catalog. The operations page aggregates all operational capabilities across queues/databases/logs.

### Adding a new capability

1. Add entry to `CAPABILITIES` array in `src/capabilities/catalog.ts`
2. Set status to `planned` (no route needed) or `ready` (with route to a page)
3. The capability automatically appears on its area landing page
4. If ready with a route, the card becomes a clickable link

### Navigation

Sidebar nav is organized by section: Overview, Investigate, Operate, Manage. Planned area links show a "soon" badge. The nav is duplicated across all HTML pages (no shared template system).

### Reusable renderers

- `src/ui/renderers/capability-card.ts` — `renderCapabilityCards(caps)` renders a grid
- `src/ui/renderers/status-badge.ts` — `renderStatusBadge(status)`, `renderRiskBadge(risk)`
- `src/ui/renderers/area-header.ts` — `renderAreaHeader(areaMeta, total, readyCount)`

## Commands

- `npm run dev` — start Vite dev server
- `npm run build` — type-check and build for production
- `npm run check:boundaries` — verify layer dependency rules
