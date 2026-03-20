import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../state/store.types";
import type { Service } from "../../types/models";
import type { TopicPayload } from "../../state/topics";
import { TOPICS } from "../../state/topics";

const MOCK_SERVICES: Service[] = [
  { id: "svc-1", name: "api-gateway", status: "healthy", uptime: "99.98%", latency: 12 },
  { id: "svc-2", name: "auth-service", status: "healthy", uptime: "99.95%", latency: 8 },
  { id: "svc-3", name: "worker-pool", status: "degraded", uptime: "97.20%", latency: 245 },
  { id: "svc-4", name: "postgres-primary", status: "healthy", uptime: "99.99%", latency: 3 },
  { id: "svc-5", name: "redis-cache", status: "healthy", uptime: "99.97%", latency: 1 },
];

let timer: ReturnType<typeof setTimeout> | null = null;

export const mockServiceHealthAdapter: DataAdapter = {
  start(store: Store) {
    timer = setTimeout(() => {
      const payload: TopicPayload<Service[]> = { status: "ok", data: MOCK_SERVICES };
      store.publish(TOPICS.SERVICES_HEALTH, payload);
    }, 400);
  },
  stop() {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
    }
  },
};
