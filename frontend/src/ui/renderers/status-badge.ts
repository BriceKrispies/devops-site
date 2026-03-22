import type { CapabilityStatus, RiskLevel } from "../../capabilities/types";

const STATUS_LABELS: Record<CapabilityStatus, string> = {
  ready: "Ready",
  stub: "Coming Soon",
  planned: "Planned",
  disabled: "Disabled",
};

const RISK_LABELS: Record<RiskLevel, string> = {
  low: "Low",
  medium: "Medium",
  high: "High",
  critical: "Critical",
};

export function renderStatusBadge(status: CapabilityStatus): string {
  return `<span class="cap-status cap-status--${status}">${STATUS_LABELS[status]}</span>`;
}

export function renderRiskBadge(risk: RiskLevel): string {
  return `<span class="cap-risk cap-risk--${risk}">${RISK_LABELS[risk]}</span>`;
}
