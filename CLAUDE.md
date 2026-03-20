# CLAUDE.md — Agent Instructions

Read `docs/` before making changes. The docs are the source of truth for this project's architecture.

## Quick reference

- **Stack:** Vite + vanilla TypeScript + plain HTML + layered CSS
- **Entry points:** `src/ui/entries/index.ts` (dashboard), `src/ui/entries/trace.ts` (trace)
- **Layers:** `platform/` → `state/` → `adapters/` → `effects/` → `ui/` (see `docs/architecture-layers.md`)
- **Store:** `src/state/store.ts` — pub/sub with `publish`, `subscribe`, `get`
- **Regions:** `src/ui/regions/region.ts` — binds `data-region` elements to store topics
- **Adapters:** `src/adapters/mock/` — mock data producers; `src/adapters/real/` — future real connectors
- **Renderers:** `src/ui/renderers/` — pure functions: `(data: T[]) => string`
- **Effects:** `src/effects/` — async orchestration (search, transforms)
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

## Commands

- `npm run dev` — start Vite dev server
- `npm run build` — type-check and build for production
- `npm run check:boundaries` — verify layer dependency rules
