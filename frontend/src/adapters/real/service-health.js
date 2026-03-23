import { TOPICS } from "../../state/topics";
import { apiFetch } from "../api-client";
/**
 * Real adapter that fetches service health from the backend API.
 * Uses the centralized apiFetch client for consistent error normalization.
 * Publishes TopicPayload with normalizedError on failure.
 */
export function createServiceHealthAdapter(baseUrl, serviceIds, pollIntervalMs = 30_000) {
    let timer = null;
    async function fetchAll(store) {
        try {
            const results = await Promise.all(serviceIds.map((id) => apiFetch(`${baseUrl}/api/health/${encodeURIComponent(id)}`)));
            const firstFailure = results.find((r) => !r.ok);
            if (firstFailure && !firstFailure.ok) {
                const payload = {
                    status: "error",
                    error: firstFailure.error.message,
                    normalizedError: firstFailure.error,
                };
                store.publish(TOPICS.SERVICES_HEALTH, payload);
                return;
            }
            const services = results
                .filter((r) => r.ok)
                .map((r) => ({
                id: r.data.serviceId,
                name: r.data.serviceId,
                status: mapStatus(r.data.status),
                uptime: "—",
                latency: 0,
            }));
            const payload = { status: "ok", data: services };
            store.publish(TOPICS.SERVICES_HEALTH, payload);
        }
        catch (err) {
            const payload = {
                status: "error",
                error: err instanceof Error ? err.message : "Unknown error",
            };
            store.publish(TOPICS.SERVICES_HEALTH, payload);
        }
    }
    return {
        start(store) {
            // Initial fetch
            fetchAll(store);
            // Poll
            if (pollIntervalMs > 0) {
                timer = setInterval(() => fetchAll(store), pollIntervalMs);
            }
        },
        stop() {
            if (timer !== null) {
                clearInterval(timer);
                timer = null;
            }
        },
    };
}
function mapStatus(status) {
    switch (status.toLowerCase()) {
        case "healthy":
            return "healthy";
        case "degraded":
            return "degraded";
        case "down":
        case "unhealthy":
            return "down";
        default:
            return "degraded";
    }
}
