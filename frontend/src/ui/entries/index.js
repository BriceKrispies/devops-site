import { createStore } from "../../state/store";
import { registerAdapters } from "../../adapters/registry";
import { bindRegion } from "../regions/region";
import { queryRegion } from "../dom";
import { TOPICS } from "../../state/topics";
import { mockServiceHealthAdapter } from "../../adapters/mock/service-health";
import { mockDeploymentsAdapter } from "../../adapters/mock/deployments";
import { mockJobsAdapter } from "../../adapters/mock/jobs";
import { renderServiceHealth } from "../renderers/service-health";
import { renderDeployments } from "../renderers/deployments";
import { renderJobs } from "../renderers/jobs";
import "../sidebar-toggle";
// Phase 2: Create store and register adapters
const store = createStore();
const teardownAdapters = registerAdapters(store, [
    mockServiceHealthAdapter,
    mockDeploymentsAdapter,
    mockJobsAdapter,
]);
// Phase 2: Bind regions to store topics
const unsubs = [];
const healthEl = queryRegion("services.health");
if (healthEl) {
    unsubs.push(bindRegion(healthEl, store, TOPICS.SERVICES_HEALTH, renderServiceHealth, "No services configured"));
}
const deploymentsEl = queryRegion("deployments.recent");
if (deploymentsEl) {
    unsubs.push(bindRegion(deploymentsEl, store, TOPICS.DEPLOYMENTS_RECENT, renderDeployments, "No recent deployments"));
}
const jobsEl = queryRegion("jobs.active");
if (jobsEl) {
    unsubs.push(bindRegion(jobsEl, store, TOPICS.JOBS_ACTIVE, renderJobs, "No active jobs"));
}
// Cleanup on page unload
window.addEventListener("unload", () => {
    for (const unsub of unsubs) {
        unsub();
    }
    teardownAdapters();
});
