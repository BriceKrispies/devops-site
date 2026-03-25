const STATUS_LABELS = {
    healthy: "Healthy",
    degraded: "Degraded",
    down: "Down",
};
export function renderServiceHealth(services) {
    return `<ul class="service-list">${services.map((s) => `<li class="service-item" data-feature="${s.id}" data-status="${s.status}">
      <span class="status-dot status-dot--${s.status}"></span>
      <span class="service-name">${s.name}</span>
      <span class="service-meta">${STATUS_LABELS[s.status]} &middot; ${s.uptime} &middot; ${s.latency}ms</span>
    </li>`).join("")}</ul>`;
}
