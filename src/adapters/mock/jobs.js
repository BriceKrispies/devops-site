import { TOPICS } from "../../state/topics";
const MOCK_JOBS = [
    { id: "job-1", name: "build-api", pipeline: "api-gateway", status: "running", duration: 47, triggeredBy: "push to main" },
    { id: "job-2", name: "integration-tests", pipeline: "auth-service", status: "queued", duration: null, triggeredBy: "merge request" },
    { id: "job-3", name: "deploy-staging", pipeline: "worker-pool", status: "passed", duration: 123, triggeredBy: "schedule" },
    { id: "job-4", name: "lint-check", pipeline: "web-frontend", status: "failed", duration: 18, triggeredBy: "push to main" },
    { id: "job-5", name: "build-images", pipeline: "api-gateway", status: "running", duration: 92, triggeredBy: "tag v2.4.1" },
];
let timer = null;
export const mockJobsAdapter = {
    start(store) {
        timer = setTimeout(() => {
            const payload = { status: "ok", data: MOCK_JOBS };
            store.publish(TOPICS.JOBS_ACTIVE, payload);
        }, 1200);
    },
    stop() {
        if (timer !== null) {
            clearTimeout(timer);
            timer = null;
        }
    },
};
