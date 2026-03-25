import { createStore } from "../../state/store";
import { registerAdapters } from "../../adapters/registry";
import { TOPICS } from "../../state/topics";
import { mockCapabilitiesAdapter } from "../../adapters/mock/capabilities";
import { realCapabilitiesAdapter } from "../../adapters/real/capabilities";
import { getByArea, getAreaMeta } from "../../capabilities/catalog";
import { toCapabilityShell } from "../../capabilities/capability-client";
import { renderCapabilityCards } from "../renderers/capability-card";
import { renderAreaHeader } from "../renderers/area-header";
import "../sidebar-toggle";
/**
 * Shared bootstrap for area landing pages.
 * Each area page calls this with its area id.
 *
 * Rendering strategy:
 * 1. Render immediately from the static catalog (instant paint, no JS blocking)
 * 2. Attempt to fetch resolved capabilities from the backend
 * 3. If backend responds, re-render with resolved state (respects kill switches, auth, overrides)
 * 4. If backend fails, the static catalog remains visible (graceful degradation)
 */
export function initAreaLanding(area) {
    const store = createStore();
    // Start both adapters: mock for instant static catalog, real for backend resolution
    const teardownAdapters = registerAdapters(store, [
        mockCapabilitiesAdapter,
        realCapabilitiesAdapter,
    ]);
    const headerEl = document.getElementById("area-header");
    const cardsEl = document.getElementById("area-capabilities");
    // Phase 1: Render immediately from static catalog (no need to wait for store)
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
    const unsubs = [];
    // Phase 2: Subscribe to resolved capabilities from backend
    unsubs.push(store.subscribe(TOPICS.RESOLVED_CAPABILITIES, (payload) => {
        if (payload.status !== "ok" || !payload.data || !cardsEl)
            return;
        // Filter to this area, convert to legacy shell format for rendering
        const areaCapabilities = payload.data
            .filter((c) => c.area === area && c.status !== "hidden")
            .map(toCapabilityShell);
        const ready = areaCapabilities.filter((c) => c.status === "ready").length;
        const contentEl = cardsEl.querySelector(".region-content");
        if (contentEl) {
            contentEl.innerHTML = renderCapabilityCards(areaCapabilities);
        }
        if (headerEl && areaMeta) {
            headerEl.innerHTML = renderAreaHeader(areaMeta, areaCapabilities.length, ready);
        }
    }));
    // Keep legacy subscription as fallback for mock-only mode
    unsubs.push(store.subscribe(TOPICS.CAPABILITIES, (payload) => {
        if (payload.status !== "ok" || !payload.data || !cardsEl)
            return;
        // Only apply if we haven't received resolved capabilities yet
        const resolved = store.get(TOPICS.RESOLVED_CAPABILITIES);
        if (resolved?.status === "ok")
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
