import { TOPICS } from "../../state/topics";
import { authApiFetch } from "../auth-api-client";
import { loadCapabilities, setCapabilitiesError, toCapabilityShells } from "../../capabilities/capability-client";
let abortController = null;
/**
 * Real capabilities adapter. Fetches resolved capabilities from GET /api/capabilities
 * and publishes to both the resolved and legacy capability topics.
 *
 * Falls back to the static catalog (via mock adapter) if the backend is unreachable,
 * so the frontend always renders something.
 */
export const realCapabilitiesAdapter = {
    start(store) {
        abortController = new AbortController();
        fetchCapabilities(store, abortController.signal);
    },
    stop() {
        if (abortController) {
            abortController.abort();
            abortController = null;
        }
    },
};
async function fetchCapabilities(store, signal) {
    const result = await authApiFetch("/api/capabilities", { signal });
    if (signal.aborted)
        return;
    if (result.ok) {
        loadCapabilities(result.data);
        // Publish resolved capabilities
        const resolvedPayload = {
            status: "ok",
            data: result.data.capabilities,
        };
        store.publish(TOPICS.RESOLVED_CAPABILITIES, resolvedPayload);
        // Also publish to the legacy capabilities topic for backward compatibility
        const legacyShells = toCapabilityShells();
        const legacyPayload = {
            status: "ok",
            data: legacyShells,
        };
        store.publish(TOPICS.CAPABILITIES, legacyPayload);
    }
    else {
        setCapabilitiesError(result.error.message);
        // Publish error to resolved topic
        const errorPayload = {
            status: "error",
            error: result.error.message,
            normalizedError: result.error,
        };
        store.publish(TOPICS.RESOLVED_CAPABILITIES, errorPayload);
    }
}
