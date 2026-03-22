# Architectural Invariants

These are hard rules. They are not suggestions. Every change to this codebase must preserve all of them. If a change would violate an invariant, the change is wrong — not the invariant.

---

### INV-1: HTML renders immediately

Every page must render a complete, meaningful shell from static HTML alone, with zero dependency on JavaScript execution or async data. If you disable JS, you see the full page structure with placeholder content.

**Test:** Load any page with JS disabled. The layout, nav, headings, and placeholder regions must all be visible.

---

### INV-2: No page depends on async data for first paint

The initial render must never be blocked by a network request, a store subscription, or an adapter response. Async data enhances the page — it does not gate it.

**Test:** Disconnect the network and load the page. The shell renders. Regions show loading placeholders.

---

### INV-3: Every async region owns its own loading state

Each `data-region` element must independently manage its own lifecycle: `loading → resolved | error | empty`. No region may delegate its loading state to another region or to a global loading indicator.

**Test:** If one adapter is slow and another is fast, the fast region resolves while the slow region keeps showing its placeholder. They are independent.

---

### INV-4: UI updates are incremental

When new data arrives on a topic, only the subscribed regions update. There is no full-page re-render. There is no DOM teardown/rebuild cycle. Updates are surgical: swap the content of the affected region.

**Test:** Publish to one topic. Only the regions subscribed to that topic change. All other regions remain untouched in the DOM.

---

### INV-5: Mocked and real adapters share the same interface

Every data adapter — mock or real — must implement the `DataAdapter` interface (`start(store)` / `stop()`). The store does not know which kind of adapter it is connected to. The UI does not know. Swapping from mock to real must require zero changes to the store or any region.

**Test:** Replace a mock adapter with a real adapter (or vice versa) in the adapter registration. The rest of the codebase compiles and runs without modification.

---

### INV-6: All data flows through the centralized store

No component, region, or module may fetch data on its own and render it directly. All data enters the system through an adapter, passes through the store via topic publish, and reaches regions via subscriptions. There are no side channels.

**Test:** Search the codebase for `fetch(`, `XMLHttpRequest`, `WebSocket(` outside of adapter modules. There must be zero occurrences.

---

### INV-7: The store is the single source of truth

The pub/sub store holds the canonical current state for every topic. Regions read from the store (via subscription). They do not cache their own copy of the data. If you need to know the current value of a topic, you ask the store.

**Test:** Call `store.get(topic)` at any point — it returns the latest published value for that topic.

---

### INV-8: State flow is centralized and observable

Every state change is visible through the store's subscription mechanism. It must be possible to subscribe to any topic and observe all updates. No state mutation happens silently.

**Test:** Subscribe to a topic with a logging callback. Every data change for that topic triggers the callback. There are no missed updates.

---

### INV-9: The main thread must not be blocked

No synchronous long-running operation may execute in the main thread during page load or during data updates. Adapters that perform heavy work must do so asynchronously (via async/await, Web Workers, or idle callbacks). The pub/sub dispatch loop must remain O(n) in the number of subscribers and must not perform DOM reads inside the dispatch.

**Test:** Run a Lighthouse performance audit. No "long task" warnings should originate from store dispatch or adapter initialization.

---

### INV-10: CSS tokens are the single source of design values

No component or layout style may use hardcoded color, spacing, radius, typography, or elevation values. All visual properties that participate in the design system must reference CSS custom properties defined in the token layer. If a value isn't in the token set, add it to the tokens — do not inline it.

**Test:** Search component and layout CSS for hardcoded hex colors, pixel spacing values, or font-size declarations that don't reference `var(--*)`. There must be zero occurrences (except within the token definitions themselves).

---

### INV-11: No framework dependencies

This frontend uses vanilla TypeScript, plain HTML, and CSS. No React, Vue, Svelte, Angular, Lit, or any other UI framework may be introduced. Vite is the build tool, not a framework.

**Test:** Check `package.json` dependencies. No UI framework packages exist.

---

### INV-12: Adapters are the only external boundary

All communication with backends, APIs, or external services happens exclusively inside adapter modules. No other module in the system may import `fetch`, create WebSocket connections, or otherwise reach outside the browser. Adapters are the firewall.

**Test:** Grep for network calls outside of `src/adapters/`. Zero results.
