const STATUS_LABELS = {
    ready: "Ready",
    stub: "Coming Soon",
    planned: "Planned",
    disabled: "Disabled",
};
const RISK_LABELS = {
    low: "Low",
    medium: "Medium",
    high: "High",
    critical: "Critical",
};
export function renderStatusBadge(status) {
    return `<span class="cap-status cap-status--${status}">${STATUS_LABELS[status]}</span>`;
}
export function renderRiskBadge(risk) {
    return `<span class="cap-risk cap-risk--${risk}">${RISK_LABELS[risk]}</span>`;
}
