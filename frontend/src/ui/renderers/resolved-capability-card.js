function escapeAttr(str) {
    return str.replace(/&/g, "&amp;").replace(/"/g, "&quot;");
}
function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}
const STATUS_LABELS = {
    enabled: "Ready",
    disabled: "Disabled",
    hidden: "Hidden",
    read_only: "Read Only",
    degraded: "Degraded",
};
const STATUS_CSS = {
    enabled: "cap-card--ready",
    disabled: "cap-card--disabled",
    hidden: "cap-card--disabled",
    read_only: "cap-card--read-only",
    degraded: "cap-card--stub",
};
function renderResolvedStatusBadge(status) {
    return `<span class="cap-status cap-status--${status}">${STATUS_LABELS[status]}</span>`;
}
function renderRiskBadge(risk) {
    return `<span class="cap-risk cap-risk--${escapeHtml(risk)}">${escapeHtml(risk.charAt(0).toUpperCase() + risk.slice(1))}</span>`;
}
function renderSingleResolvedCard(cap) {
    const isNavigable = cap.status === "enabled" && cap.route;
    const tag = isNavigable ? "a" : "div";
    const href = isNavigable ? ` href="${escapeAttr(cap.route)}"` : "";
    const statusClass = STATUS_CSS[cap.status] ?? "cap-card--disabled";
    const riskClass = cap.risk !== "low" ? ` cap-card--risk-${escapeAttr(cap.risk)}` : "";
    const messageHtml = cap.message && cap.status !== "enabled"
        ? `<p class="cap-card-message">${escapeHtml(cap.message)}</p>`
        : "";
    return `<${tag} class="cap-card ${statusClass}${riskClass}" data-feature="${escapeAttr(cap.key)}" data-status="${cap.status}"${href}>
    <div class="cap-card-header">
      <span class="cap-card-name">${escapeHtml(cap.name)}</span>
      <div class="cap-card-badges">
        ${cap.risk !== "low" ? renderRiskBadge(cap.risk) : ""}
        ${renderResolvedStatusBadge(cap.status)}
      </div>
    </div>
    <p class="cap-card-desc">${escapeHtml(cap.description)}</p>
    ${messageHtml}
    ${cap.permissions.length > 0
        ? `<div class="cap-card-perms">${cap.permissions.map((p) => `<span class="cap-perm">${escapeHtml(p)}</span>`).join("")}</div>`
        : ""}
  </${tag}>`;
}
/**
 * Render resolved capabilities as a card grid.
 * Uses the full resolved status model for richer display.
 */
export function renderResolvedCapabilityCards(capabilities) {
    // Filter out hidden capabilities
    const visible = capabilities.filter((c) => c.status !== "hidden");
    if (visible.length === 0) {
        return `<div class="region-empty"><p>No capabilities available in this area.</p></div>`;
    }
    return `<div class="cap-grid">${visible.map(renderSingleResolvedCard).join("")}</div>`;
}
