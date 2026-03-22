import type { CapabilityShell } from "../../capabilities/types";
import type { TopicPayload } from "../../state/topics";
import { createStore } from "../../state/store";
import { registerAdapters } from "../../adapters/registry";
import { TOPICS } from "../../state/topics";
import { mockCapabilitiesAdapter } from "../../adapters/mock/capabilities";
import { getAll } from "../../capabilities/catalog";
import { renderCapabilityCards } from "../renderers/capability-card";
import "../sidebar-toggle";

// Operations page shows ALL operational capabilities across areas
const OPERATIONAL_AREAS = new Set(["queues", "databases", "logs", "operations"]);

const store = createStore();
const teardownAdapters = registerAdapters(store, [mockCapabilitiesAdapter]);

const cardsEl = document.getElementById("area-capabilities");

// Render immediately from static catalog
const allCaps = getAll().filter((c) => OPERATIONAL_AREAS.has(c.area)) as CapabilityShell[];
if (cardsEl) {
  cardsEl.dataset.state = "resolved";
  const contentEl = cardsEl.querySelector<HTMLElement>(".region-content");
  if (contentEl) {
    contentEl.innerHTML = renderCapabilityCards(allCaps);
  }
}

const unsub = store.subscribe<TopicPayload<CapabilityShell[]>>(TOPICS.CAPABILITIES, (payload) => {
  if (payload.status !== "ok" || !payload.data || !cardsEl) return;
  const filtered = payload.data.filter((c) => OPERATIONAL_AREAS.has(c.area));
  const contentEl = cardsEl.querySelector<HTMLElement>(".region-content");
  if (contentEl) {
    contentEl.innerHTML = renderCapabilityCards(filtered);
  }
});

window.addEventListener("unload", () => {
  unsub();
  teardownAdapters();
});
