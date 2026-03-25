import { requireAuth } from "../../platform/auth-guard";
import { createStore } from "../../state/store";
import { registerAdapters } from "../../adapters/registry";
import { TOPICS } from "../../state/topics";
import { mockCapabilitiesAdapter } from "../../adapters/mock/capabilities";
import { getAll } from "../../capabilities/catalog";
import { renderCapabilityCards } from "../renderers/capability-card";
import "../sidebar-toggle";
requireAuth().then((ok) => {
    if (!ok)
        return;
    const OPERATIONAL_AREAS = new Set(["queues", "databases", "logs", "operations"]);
    const store = createStore();
    const teardownAdapters = registerAdapters(store, [mockCapabilitiesAdapter]);
    const cardsEl = document.getElementById("area-capabilities");
    const allCaps = getAll().filter((c) => OPERATIONAL_AREAS.has(c.area));
    if (cardsEl) {
        cardsEl.dataset.state = "resolved";
        const contentEl = cardsEl.querySelector(".region-content");
        if (contentEl) {
            contentEl.innerHTML = renderCapabilityCards(allCaps);
        }
    }
    const unsub = store.subscribe(TOPICS.CAPABILITIES, (payload) => {
        if (payload.status !== "ok" || !payload.data || !cardsEl)
            return;
        const filtered = payload.data.filter((c) => OPERATIONAL_AREAS.has(c.area));
        const contentEl = cardsEl.querySelector(".region-content");
        if (contentEl) {
            contentEl.innerHTML = renderCapabilityCards(filtered);
        }
    });
    window.addEventListener("unload", () => {
        unsub();
        teardownAdapters();
    });
});
