import { createStore } from "../../state/store";
import { registerAdapters } from "../../adapters/registry";
import { TOPICS } from "../../state/topics";
import { mockCapabilitiesAdapter } from "../../adapters/mock/capabilities";
import { getByArea, getAreaMeta } from "../../capabilities/catalog";
import { renderCapabilityCards } from "../renderers/capability-card";
import { renderAreaHeader } from "../renderers/area-header";
import "../sidebar-toggle";
/**
 * Shared bootstrap for area landing pages.
 * Each area page calls this with its area id.
 */
export function initAreaLanding(area) {
    const store = createStore();
    const teardownAdapters = registerAdapters(store, [mockCapabilitiesAdapter]);
    const headerEl = document.getElementById("area-header");
    const cardsEl = document.getElementById("area-capabilities");
    // Render immediately from static catalog (no need to wait for store)
    const areaMeta = getAreaMeta(area);
    const capabilities = getByArea(area);
    const readyCount = capabilities.filter((c) => c.status === "ready").length;
    if (headerEl && areaMeta) {
        headerEl.innerHTML = renderAreaHeader(areaMeta, capabilities.length, readyCount);
    }
    if (cardsEl) {
        cardsEl.dataset.state = "resolved";
        const contentEl = cardsEl.querySelector(".region-content");
        if (contentEl) {
            contentEl.innerHTML = renderCapabilityCards(capabilities);
        }
    }
    // Also subscribe to store for future dynamic updates
    const unsubs = [];
    unsubs.push(store.subscribe(TOPICS.CAPABILITIES, (payload) => {
        if (payload.status !== "ok" || !payload.data || !cardsEl)
            return;
        const filtered = payload.data.filter((c) => c.area === area);
        const ready = filtered.filter((c) => c.status === "ready").length;
        const contentEl = cardsEl.querySelector(".region-content");
        if (contentEl) {
            contentEl.innerHTML = renderCapabilityCards(filtered);
        }
        if (headerEl && areaMeta) {
            headerEl.innerHTML = renderAreaHeader(areaMeta, filtered.length, ready);
        }
    }));
    return () => {
        for (const unsub of unsubs)
            unsub();
        teardownAdapters();
    };
}
