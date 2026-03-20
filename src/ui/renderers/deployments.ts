import type { Deployment } from "../../types/models";

export function renderDeployments(deployments: Deployment[]): string {
  return `<table class="deploy-table">
    <thead>
      <tr>
        <th>Service</th>
        <th>Env</th>
        <th>Status</th>
        <th>Commit</th>
        <th>When</th>
      </tr>
    </thead>
    <tbody>${deployments.map((d) =>
      `<tr>
        <td>${d.service}</td>
        <td><span class="badge badge--env">${d.environment}</span></td>
        <td><span class="badge badge--${d.status}">${d.status}</span></td>
        <td><code>${d.commit}</code></td>
        <td class="text-muted">${d.timestamp}</td>
      </tr>`
    ).join("")}</tbody>
  </table>`;
}
