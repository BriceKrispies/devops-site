function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}
export function renderAreaHeader(area, capCount, readyCount) {
    return `<div class="area-header">
    <div class="area-header-text">
      <h1 class="area-header-title">${escapeHtml(area.name)}</h1>
      <p class="area-header-desc">${escapeHtml(area.description)}</p>
    </div>
    <div class="area-header-stats">
      <span class="area-stat">${readyCount}/${capCount} ready</span>
    </div>
  </div>`;
}
