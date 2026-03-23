import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../state/store.types";
import type { Service } from "../../types/models";
import type { TopicPayload } from "../../state/topics";
import { TOPICS } from "../../state/topics";
import { apiFetch } from "../api-client";

interface ServiceHealthApiResponse {
  serviceId: string;
  status: string;
  description: string;
  checkedAt: string;
}

/**
 * Real adapter that fetches service health from the backend API.
 * Uses the centralized apiFetch client for consistent error normalization.
 * Publishes TopicPayload with normalizedError on failure.
 */
export function createServiceHealthAdapter(
  baseUrl: string,
  serviceIds: string[],
  pollIntervalMs = 30_000,
): DataAdapter {
  let timer: ReturnType<typeof setInterval> | null = null;

  async function fetchAll(store: Store): Promise<void> {
    try {
      const results = await Promise.all(
        serviceIds.map((id) =>
          apiFetch<ServiceHealthApiResponse>(`${baseUrl}/api/health/${encodeURIComponent(id)}`),
        ),
      );

      const firstFailure = results.find((r) => !r.ok);
      if (firstFailure && !firstFailure.ok) {
        const payload: TopicPayload<Service[]> = {
          status: "error",
          error: firstFailure.error.message,
          normalizedError: firstFailure.error,
        };
        store.publish(TOPICS.SERVICES_HEALTH, payload);
        return;
      }

      const services: Service[] = results
        .filter((r): r is Extract<typeof r, { ok: true }> => r.ok)
        .map((r) => ({
          id: r.data.serviceId,
          name: r.data.serviceId,
          status: mapStatus(r.data.status),
          uptime: "—",
          latency: 0,
        }));

      const payload: TopicPayload<Service[]> = { status: "ok", data: services };
      store.publish(TOPICS.SERVICES_HEALTH, payload);
    } catch (err) {
      const payload: TopicPayload<Service[]> = {
        status: "error",
        error: err instanceof Error ? err.message : "Unknown error",
      };
      store.publish(TOPICS.SERVICES_HEALTH, payload);
    }
  }

  return {
    start(store: Store) {
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

function mapStatus(status: string): Service["status"] {
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
