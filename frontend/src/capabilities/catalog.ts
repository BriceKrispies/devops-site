import type { CapabilityShell, FeatureArea, FeatureAreaMeta } from "./types";

// ──────────────────────────────────────────────────────────────
//  Area metadata
// ──────────────────────────────────────────────────────────────

export const AREA_META: readonly FeatureAreaMeta[] = [
  { id: "overview", name: "Overview", description: "System health, deployments, and active jobs.", navSection: "Overview", route: "/" },
  { id: "investigate", name: "Investigate", description: "Trace and correlate events across systems.", navSection: "Investigate", route: null },
  { id: "queues", name: "Queues", description: "SQS queue monitoring, DLQ management, and message operations.", navSection: "Operate", route: "/src/pages/queues.html" },
  { id: "databases", name: "Databases", description: "RDS instance health, replication status, and non-prod cloning.", navSection: "Operate", route: "/src/pages/databases.html" },
  { id: "logs", name: "Logs", description: "CloudWatch log search, filtering, and exploration.", navSection: "Operate", route: "/src/pages/logs.html" },
  { id: "operations", name: "Operations", description: "Unified view of all operational capabilities.", navSection: "Manage", route: "/src/pages/operations.html" },
  { id: "admin", name: "Admin", description: "Permissions, roles, and system configuration.", navSection: "Manage", route: "/src/pages/admin.html" },
] as const;

// ──────────────────────────────────────────────────────────────
//  Capability catalog
// ──────────────────────────────────────────────────────────────

const CAPABILITIES: readonly CapabilityShell[] = [
  // ── Overview (ready) ──
  {
    id: "Dashboard",
    name: "Dashboard",
    area: "overview",
    description: "Service health, deployments, and active jobs at a glance.",
    status: "ready",
    risk: "low",
    route: "/",
    permissions: ["servicehealth:read", "workitem:read"],
  },

  // ── Investigate (ready) ──
  {
    id: "QueryTraceEvents",
    name: "Trace Explorer",
    area: "investigate",
    description: "Search and correlate events across Jira, GitHub, and CI.",
    status: "ready",
    risk: "low",
    route: "/src/pages/trace.html",
    permissions: ["traceevents:read"],
  },

  // ── Queues (planned) ──
  {
    id: "QueuesRead",
    name: "Queue Status",
    area: "queues",
    description: "View SQS queue depths, message ages, and DLQ counts.",
    status: "planned",
    risk: "low",
    route: null,
    permissions: ["queues:read"],
  },
  {
    id: "QueuesRedriveDlq",
    name: "DLQ Redrive",
    area: "queues",
    description: "Redrive messages from dead-letter queues back to source queues.",
    status: "planned",
    risk: "high",
    route: null,
    permissions: ["queues:operate"],
  },

  // ── Databases (planned) ──
  {
    id: "DatabasesRead",
    name: "Database Status",
    area: "databases",
    description: "View RDS instance health, connections, and replication lag.",
    status: "planned",
    risk: "low",
    route: null,
    permissions: ["databases:read"],
  },
  {
    id: "DatabasesCloneNonProd",
    name: "Clone Non-Prod",
    area: "databases",
    description: "Clone production snapshots to non-production environments.",
    status: "planned",
    risk: "critical",
    route: null,
    permissions: ["databases:operate"],
  },

  // ── Logs (planned) ──
  {
    id: "LogsRead",
    name: "Log Explorer",
    area: "logs",
    description: "Search and filter CloudWatch log groups and streams.",
    status: "planned",
    risk: "low",
    route: null,
    permissions: ["logs:read"],
  },

  // ── Operations (planned) ──
  {
    id: "OpsRunbooks",
    name: "Runbooks",
    area: "operations",
    description: "Execute operational runbooks with full audit trail.",
    status: "planned",
    risk: "high",
    route: null,
    permissions: [],
  },

  // ── Admin (planned) ──
  {
    id: "AdminPermissions",
    name: "Permissions",
    area: "admin",
    description: "View and manage user permissions and roles.",
    status: "planned",
    risk: "high",
    route: null,
    permissions: [],
  },
  {
    id: "AdminAuditLog",
    name: "Audit Log",
    area: "admin",
    description: "View the audit trail of privileged operations.",
    status: "planned",
    risk: "low",
    route: null,
    permissions: [],
  },
] as const;

// ──────────────────────────────────────────────────────────────
//  Catalog queries
// ──────────────────────────────────────────────────────────────

/** All registered capabilities. */
export function getAll(): readonly CapabilityShell[] {
  return CAPABILITIES;
}

/** Capabilities filtered by feature area. */
export function getByArea(area: FeatureArea): readonly CapabilityShell[] {
  return CAPABILITIES.filter((c) => c.area === area);
}

/** Capabilities filtered by status. */
export function getByStatus(status: CapabilityShell["status"]): readonly CapabilityShell[] {
  return CAPABILITIES.filter((c) => c.status === status);
}

/** Single capability by id, or undefined. */
export function getById(id: string): CapabilityShell | undefined {
  return CAPABILITIES.find((c) => c.id === id);
}

/** Area metadata by area id. */
export function getAreaMeta(area: FeatureArea): FeatureAreaMeta | undefined {
  return AREA_META.find((a) => a.id === area);
}
