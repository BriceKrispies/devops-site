export function renderJobs(jobs) {
    return `<ul class="job-list">${jobs.map((j) => `<li class="job-item" data-feature="${j.id}" data-status="${j.status}">
      <div class="job-header">
        <span class="job-status job-status--${j.status}"></span>
        <span class="job-name">${j.name}</span>
        <span class="badge badge--env">${j.pipeline}</span>
      </div>
      <div class="job-meta">
        ${j.duration !== null ? `${j.duration}s` : "waiting"} &middot; ${j.triggeredBy}
      </div>
    </li>`).join("")}</ul>`;
}
