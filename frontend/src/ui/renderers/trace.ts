import type { TraceEvent } from "../../types/models";

const SOURCE_LABELS: Record<string, string> = {
  jira: "JIRA",
  github: "GH",
  ci: "CI",
};

function formatTime(iso: string): string {
  const d = new Date(iso);
  const h = d.getUTCHours().toString().padStart(2, "0");
  const m = d.getUTCMinutes().toString().padStart(2, "0");
  const s = d.getUTCSeconds().toString().padStart(2, "0");
  return `${h}:${m}:${s}`;
}

function renderEntityIds(ids: Record<string, string>): string {
  return Object.entries(ids)
    .map(([k, v]) => `<span class="trace-entity">${k}=${v}</span>`)
    .join("");
}

export function renderTrace(events: TraceEvent[]): string {
  return `<ol class="trace-timeline">${events.map((e) =>
    `<li class="trace-entry trace-entry--${e.source}" data-feature="${e.id}">
      <span class="trace-time">${formatTime(e.timestamp)}</span>
      <span class="trace-source trace-source--${e.source}">${SOURCE_LABELS[e.source] ?? e.source}</span>
      <span class="trace-type">${e.type}</span>
      <span class="trace-summary">${e.summary}</span>
      <span class="trace-entities">${renderEntityIds(e.entityIds)}</span>
    </li>`
  ).join("")}</ol>`;
}
