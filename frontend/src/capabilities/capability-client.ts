import type { ResolvedCapability, ResolvedStatus, CapabilitiesResponse } from "./resolved-types";
import type { CapabilityShell, FeatureArea } from "./types";
import { resolvedStatusToLegacy } from "./resolved-types";

/**
 * Central capability accessor. All frontend code should use this layer
 * instead of checking capability strings/statuses directly.
 *
 * The client holds the last-known resolved capabilities from the backend.
 * It provides typed, safe access with fail-closed defaults.
 */

let _capabilities: Map<string, ResolvedCapability> = new Map();
let _loaded = false;
let _error: string | null = null;

/** Whether capabilities have been loaded from the backend. */
export function isLoaded(): boolean {
  return _loaded;
}

/** Whether loading failed. */
export function hasError(): boolean {
  return _error !== null;
}

/** The error message if loading failed. */
export function getError(): string | null {
  return _error;
}

/**
 * Load capabilities from the provided response data.
 * Called by the capabilities adapter when data arrives.
 */
export function loadCapabilities(response: CapabilitiesResponse): void {
  _capabilities = new Map();
  for (const cap of response.capabilities) {
    _capabilities.set(cap.key, cap);
  }
  _loaded = true;
  _error = null;
}

/** Mark capabilities as failed to load. */
export function setCapabilitiesError(error: string): void {
  _error = error;
  _loaded = false;
}

/** Reset to initial state. */
export function resetCapabilities(): void {
  _capabilities = new Map();
  _loaded = false;
  _error = null;
}

/**
 * Get the resolved status of a capability.
 * Returns "disabled" if the capability is unknown or not loaded (fail closed).
 */
export function getStatus(key: string): ResolvedStatus {
  if (!_loaded) return "disabled";
  const cap = _capabilities.get(key);
  return cap?.status ?? "disabled";
}

/**
 * Check if a capability is enabled for the current user.
 */
export function isEnabled(key: string): boolean {
  return getStatus(key) === "enabled";
}

/**
 * Check if a capability is visible (not hidden).
 * Hidden capabilities should not be rendered at all.
 */
export function isVisible(key: string): boolean {
  const status = getStatus(key);
  return status !== "hidden";
}

/**
 * Check if a capability allows user actions (not read-only or disabled).
 */
export function isActionable(key: string): boolean {
  const status = getStatus(key);
  return status === "enabled" || status === "degraded";
}

/**
 * Get the full resolved capability, or null if not found.
 */
export function getCapability(key: string): ResolvedCapability | null {
  return _capabilities.get(key) ?? null;
}

/**
 * Get the user-facing message for a capability's current state.
 */
export function getMessage(key: string): string | null {
  return _capabilities.get(key)?.message ?? null;
}

/**
 * Get all resolved capabilities for a feature area.
 * Excludes hidden capabilities.
 */
export function getByArea(area: FeatureArea | string): ResolvedCapability[] {
  const results: ResolvedCapability[] = [];
  for (const cap of _capabilities.values()) {
    if (cap.area === area && cap.status !== "hidden") {
      results.push(cap);
    }
  }
  return results;
}

/**
 * Get all resolved capabilities.
 * Excludes hidden capabilities.
 */
export function getAllVisible(): ResolvedCapability[] {
  const results: ResolvedCapability[] = [];
  for (const cap of _capabilities.values()) {
    if (cap.status !== "hidden") {
      results.push(cap);
    }
  }
  return results;
}

/**
 * Convert a resolved capability to the legacy CapabilityShell format
 * for compatibility with existing renderers.
 */
export function toCapabilityShell(cap: ResolvedCapability): CapabilityShell {
  return {
    id: cap.key,
    name: cap.name,
    area: cap.area as FeatureArea,
    description: cap.description,
    status: resolvedStatusToLegacy(cap.status),
    risk: cap.risk as "low" | "medium" | "high" | "critical",
    route: cap.route,
    permissions: cap.permissions,
  };
}

/**
 * Convert resolved capabilities to legacy CapabilityShell[] format.
 * Filters out hidden capabilities.
 */
export function toCapabilityShells(area?: FeatureArea | string): CapabilityShell[] {
  const caps = area ? getByArea(area) : getAllVisible();
  return caps.map(toCapabilityShell);
}
