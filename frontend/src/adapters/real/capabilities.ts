import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../state/store.types";
import type { TopicPayload } from "../../state/topics";
import type { ResolvedCapability, CapabilitiesResponse } from "../../capabilities/resolved-types";
import type { CapabilityShell } from "../../capabilities/types";
import { TOPICS } from "../../state/topics";
import { apiFetch } from "../api-client";
import { loadCapabilities, setCapabilitiesError, toCapabilityShells } from "../../capabilities/capability-client";

let abortController: AbortController | null = null;

/**
 * Real capabilities adapter. Fetches resolved capabilities from GET /api/capabilities
 * and publishes to both the resolved and legacy capability topics.
 *
 * Falls back to the static catalog (via mock adapter) if the backend is unreachable,
 * so the frontend always renders something.
 */
export const realCapabilitiesAdapter: DataAdapter = {
  start(store: Store) {
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

async function fetchCapabilities(store: Store, signal: AbortSignal): Promise<void> {
  const result = await apiFetch<CapabilitiesResponse>("/api/capabilities", { signal });

  if (signal.aborted) return;

  if (result.ok) {
    loadCapabilities(result.data);

    // Publish resolved capabilities
    const resolvedPayload: TopicPayload<ResolvedCapability[]> = {
      status: "ok",
      data: result.data.capabilities,
    };
    store.publish(TOPICS.RESOLVED_CAPABILITIES, resolvedPayload);

    // Also publish to the legacy capabilities topic for backward compatibility
    const legacyShells = toCapabilityShells();
    const legacyPayload: TopicPayload<CapabilityShell[]> = {
      status: "ok",
      data: legacyShells,
    };
    store.publish(TOPICS.CAPABILITIES, legacyPayload);
  } else {
    setCapabilitiesError(result.error.message);

    // Publish error to resolved topic
    const errorPayload: TopicPayload<ResolvedCapability[]> = {
      status: "error",
      error: result.error.message,
      normalizedError: result.error,
    };
    store.publish(TOPICS.RESOLVED_CAPABILITIES, errorPayload);
  }
}
