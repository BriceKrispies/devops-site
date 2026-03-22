# Agent Guide

Instructions for AI coding agents working in this repository.

## Before You Change Anything

1. **Read `docs/invariants.md`.** Every change must preserve all invariants. If you're unsure whether a change violates an invariant, it probably does.
2. **Read `docs/architecture.md` and `docs/architecture-layers.md`.** Understand the layer boundaries and how data flows between them.
3. **Read `docs/project-structure.md`.** Know where files belong before creating new ones.

## How to Add a New Page

1. Create `src/pages/<name>.html` with the full page shell, including nav, layout, and placeholder regions (with skeleton markup) for any async content.
2. Create `src/ui/entries/<name>.ts` as the entry module. Import the store, register adapters, wire effects, bind regions.
3. Add a `<script type="module" src="/src/ui/entries/<name>.ts">` tag to the HTML file.
4. Verify: the page renders its full shell with JS disabled.

## How to Add a New Async Region

1. Add a `<section data-region="<topic>" data-state="loading">` element to the HTML file with skeleton placeholder markup inside.
2. Create a renderer function in `src/ui/renderers/<topic>.ts` that takes the data shape and returns an HTML string.
3. In the page's entry module, bind the region to the store topic using the renderer.
4. Verify: the region shows a skeleton on load, then transitions to resolved/error/empty when data arrives.

## How to Add a New Mock Adapter

1. Create `src/adapters/mock/<topic>.ts`.
2. Implement the `DataAdapter` interface: `start(store)` and `stop()`.
3. In `start()`, publish mock data to the appropriate topic. Use `setTimeout` to simulate async delay (200–800ms is realistic).
4. Register the adapter in the relevant page entry module.
5. Verify: the region subscribed to this topic receives data and renders it.

## How to Add a Real Backend Connector

1. Create `src/adapters/real/<topic>.ts`.
2. Implement the same `DataAdapter` interface.
3. In `start()`, fetch from the real API and publish to the same topic the mock adapter uses.
4. In `stop()`, abort any in-flight requests or close connections.
5. In the page entry module, swap the mock adapter import for the real adapter import.
6. Verify: the rest of the codebase requires zero changes. The store, regions, and renderers are untouched.

## How to Add a New Event/Topic

1. Define the topic name and payload type in `src/state/topics.ts`.
2. Create the adapter that will publish to this topic.
3. Create the renderer for any region that will display this topic's data.
4. Bind the region in the appropriate entry module.

## What Good Changes Look Like

- **Small.** Touch as few files as possible.
- **Layer-respecting.** Adapters don't touch the DOM. Renderers don't import the store. Regions don't fetch data. Run `npm run check:boundaries`.
- **Invariant-preserving.** Every invariant in `docs/invariants.md` holds after the change.
- **Tested visually.** The page renders with JS disabled (shell check). The page renders with a slow/failing adapter (loading/error state check).
- **Token-using.** New CSS references design tokens, not hardcoded values.

## What Bad Changes Look Like

- Adding a `fetch()` call inside a renderer or region binding.
- Creating a region that doesn't have a loading placeholder in the HTML.
- Hardcoding a color or spacing value in CSS.
- Publishing to a topic from two different adapters.
- Creating a file outside the established folder structure without justification.
- Adding an external UI framework dependency.
- Writing a region that depends on another region's state.

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Forgetting skeleton markup in HTML | Every `data-region` needs inline placeholder content |
| Not handling the `error` state | Every region subscriber must check `payload.status` |
| Not calling `unsubscribe()` on teardown | Store leaks. Always clean up subscriptions |
| Putting network calls outside adapters | All external communication lives in `src/adapters/` |
| Putting expensive logic in renderers | Filtering/sorting/correlation goes in `src/effects/` |
| Importing across layer boundaries | Run `npm run check:boundaries` to catch violations |
| Creating deeply nested folders | Keep it flat. One level of nesting under `src/` is usually enough |
| Adding comments that restate the code | Only comment non-obvious intent |

## Tone and Style

- Write TypeScript, not JavaScript. Use strict types.
- Prefer explicit over clever.
- Keep functions short. If a function needs a comment explaining what it does, it should probably be two functions.
- No `any` types. If you can't type it, you don't understand it yet.
- Use `const` by default. Use `let` only when reassignment is necessary. Never use `var`.
