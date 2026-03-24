import { requireAuth } from "../../platform/auth-guard";
import { createStore } from "../../state/store";
import { registerAdapters } from "../../adapters/registry";
import { TOPICS } from "../../state/topics";
import type { TopicPayload } from "../../state/topics";
import type { TraceEvent } from "../../types/models";

import { mockTraceAdapter } from "../../adapters/mock/trace";
import { createTraceSearchEffect } from "../../effects/trace-search";
import { renderTrace } from "../renderers/trace";
import "../sidebar-toggle";

requireAuth().then((ok) => {
  if (!ok) return;

  const store = createStore();

  const teardownAdapters = registerAdapters(store, [
    mockTraceAdapter,
  ]);

  const teardownSearch = createTraceSearchEffect(store);

  const form = document.getElementById("trace-search-form") as HTMLFormElement;
  const input = document.getElementById("trace-query") as HTMLInputElement;
  const prompt = document.getElementById("trace-prompt") as HTMLElement;
  const resultsEl = document.getElementById("trace-results") as HTMLElement;
  const loadingEl = resultsEl.querySelector<HTMLElement>(".region-loading")!;
  const contentEl = resultsEl.querySelector<HTMLElement>(".region-content")!;

  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const query = input.value.trim();
    if (query.length === 0) return;

    prompt.style.display = "none";
    loadingEl.style.display = "block";
    contentEl.style.display = "none";
    resultsEl.dataset.state = "loading";

    store.publish(TOPICS.TRACE_QUERY, query);
  });

  const unsub = store.subscribe<TopicPayload<TraceEvent[]>>(TOPICS.TRACE_RESULTS, (payload) => {
    loadingEl.style.display = "none";
    contentEl.style.display = "block";

    if (payload.status === "error") {
      resultsEl.dataset.state = "error";
      contentEl.innerHTML = `<div class="region-error"><p>${payload.error ?? "Search failed"}</p></div>`;
    } else if (!payload.data || payload.data.length === 0) {
      resultsEl.dataset.state = "empty";
      contentEl.innerHTML = `<div class="trace-empty">No events found for this query.</div>`;
    } else {
      resultsEl.dataset.state = "resolved";
      contentEl.innerHTML = renderTrace(payload.data);
    }
  });

  input.focus();

  window.addEventListener("unload", () => {
    unsub();
    teardownSearch();
    teardownAdapters();
  });
});
