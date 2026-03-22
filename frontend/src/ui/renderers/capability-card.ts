import type { CapabilityShell } from "../../capabilities/types";
import { renderStatusBadge, renderRiskBadge } from "./status-badge";

function escapeAttr(str: string): string {
  return str.replace(/&/g, "&amp;").replace(/"/g, "&quot;");
}

function escapeHtml(str: string): string {
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}

function renderSingleCard(cap: CapabilityShell): string {
  const isNavigable = cap.status === "ready" && cap.route;
  const tag = isNavigable ? "a" : "div";
  const href = isNavigable ? ` href="${escapeAttr(cap.route!)}"` : "";
  const statusClass = `cap-card--${cap.status}`;
  const riskClass = cap.risk !== "low" ? ` cap-card--risk-${cap.risk}` : "";

  return `<${tag} class="cap-card ${statusClass}${riskClass}"${href}>
    <div class="cap-card-header">
      <span class="cap-card-name">${escapeHtml(cap.name)}</span>
      <div class="cap-card-badges">
        ${cap.risk !== "low" ? renderRiskBadge(cap.risk) : ""}
        ${renderStatusBadge(cap.status)}
      </div>
    </div>
    <p class="cap-card-desc">${escapeHtml(cap.description)}</p>
    ${cap.permissions.length > 0
      ? `<div class="cap-card-perms">${cap.permissions.map((p) => `<span class="cap-perm">${escapeHtml(p)}</span>`).join("")}</div>`
      : ""}
  </${tag}>`;
}

export function renderCapabilityCards(capabilities: CapabilityShell[]): string {
  if (capabilities.length === 0) {
    return `<div class="region-empty"><p>No capabilities in this area.</p></div>`;
  }
  return `<div class="cap-grid">${capabilities.map(renderSingleCard).join("")}</div>`;
}
