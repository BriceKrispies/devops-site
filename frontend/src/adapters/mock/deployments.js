import { TOPICS } from "../../state/topics";
const MOCK_DEPLOYMENTS = [
    { id: "dep-1", service: "api-gateway", environment: "production", status: "success", timestamp: "2 min ago", commit: "a3f8c21" },
    { id: "dep-2", service: "auth-service", environment: "staging", status: "rolling", timestamp: "8 min ago", commit: "e7b4d09" },
    { id: "dep-3", service: "worker-pool", environment: "production", status: "failed", timestamp: "23 min ago", commit: "1c9f3a7" },
    { id: "dep-4", service: "web-frontend", environment: "production", status: "success", timestamp: "1 hr ago", commit: "b2d6e45" },
    { id: "dep-5", service: "postgres-primary", environment: "staging", status: "success", timestamp: "3 hr ago", commit: "8f1a2c3" },
];
let timer = null;
export const mockDeploymentsAdapter = {
    start(store) {
        timer = setTimeout(() => {
            const payload = { status: "ok", data: MOCK_DEPLOYMENTS };
            store.publish(TOPICS.DEPLOYMENTS_RECENT, payload);
        }, 800);
    },
    stop() {
        if (timer !== null) {
            clearTimeout(timer);
            timer = null;
        }
    },
};
