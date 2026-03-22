export function renderDeployments(deployments) {
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
    <tbody>${deployments.map((d) => `<tr>
        <td data-label="Service">${d.service}</td>
        <td data-label="Env"><span class="badge badge--env">${d.environment}</span></td>
        <td data-label="Status"><span class="badge badge--${d.status}">${d.status}</span></td>
        <td data-label="Commit"><code>${d.commit}</code></td>
        <td data-label="When" class="text-muted">${d.timestamp}</td>
      </tr>`).join("")}</tbody>
  </table>`;
}
