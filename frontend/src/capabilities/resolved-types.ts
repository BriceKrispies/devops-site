/**
 * Resolved capability status as returned by the backend.
 * This replaces the static CapabilityStatus for rendering decisions.
 */
export type ResolvedStatus =
  | "enabled"
  | "disabled"
  | "hidden"
  | "read_only"
  | "degraded";

/** Metadata attached to a resolved capability. */
export interface ResolvedCapabilityMetadata {
  category: string | null;
  privileged: boolean;
  executionMode: string | null;
}

/**
 * A capability resolved by the backend for the current user/session.
 * This is the single contract between backend and frontend for feature control.
 */
export interface ResolvedCapability {
  /** Unique capability identifier (matches backend OperationName). */
  key: string;
  /** Runtime-resolved status after all resolution rules applied. */
  status: ResolvedStatus;
  /** Human-readable display name. */
  name: string;
  /** Frontend feature area grouping. */
  area: string;
  /** One-line description. */
  description: string;
  /** Risk classification. */
  risk: string;
  /** Frontend route, or null if not navigable. */
  route: string | null;
  /** Optional user-facing message explaining the status. */
  message: string | null;
  /** Machine-readable reason for the status. */
  reason: string | null;
  /** Required permission strings. */
  permissions: string[];
  /** Additional metadata. */
  metadata: ResolvedCapabilityMetadata | null;
}

/** The API response shape from GET /api/capabilities. */
export interface CapabilitiesResponse {
  capabilities: ResolvedCapability[];
}

/**
 * Maps a ResolvedStatus to the legacy CapabilityStatus used by existing renderers.
 * This allows gradual migration — new code uses ResolvedStatus directly,
 * existing renderers can continue using the legacy type.
 */
export function resolvedStatusToLegacy(status: ResolvedStatus): "ready" | "stub" | "planned" | "disabled" {
  switch (status) {
    case "enabled":
      return "ready";
    case "degraded":
      return "stub";
    case "disabled":
      return "disabled";
    case "hidden":
      return "disabled";
    case "read_only":
      return "ready";
    default:
      return "disabled";
  }
}
