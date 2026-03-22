/** Feature area groupings for the portal navigation and capability organization. */
export type FeatureArea =
  | "overview"
  | "investigate"
  | "queues"
  | "databases"
  | "logs"
  | "operations"
  | "admin";

/** Mirrors the backend ImplementationStatus enum. */
export type CapabilityStatus = "planned" | "stub" | "ready" | "disabled";

/** Mirrors the backend RiskLevel enum. */
export type RiskLevel = "low" | "medium" | "high" | "critical";

/**
 * Frontend capability shell — the minimal metadata needed to render
 * status-aware cards, gates, and navigation without any implementation.
 */
export interface CapabilityShell {
  /** Unique identifier matching the backend OperationName where applicable. */
  id: string;
  /** Human-readable display name. */
  name: string;
  /** Which feature area this capability belongs to. */
  area: FeatureArea;
  /** One-line description of what the capability does. */
  description: string;
  /** Current implementation status. */
  status: CapabilityStatus;
  /** Risk classification from the backend capability model. */
  risk: RiskLevel;
  /** Route to the capability page, or null if not yet navigable. */
  route: string | null;
  /** Backend permission strings required (for future gating). */
  permissions: string[];
}

/** Area metadata for rendering area landing pages. */
export interface FeatureAreaMeta {
  id: FeatureArea;
  name: string;
  description: string;
  navSection: string;
  route: string | null;
}
