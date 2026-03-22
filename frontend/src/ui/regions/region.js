function setRegionState(el, state) {
    el.dataset.state = state;
}
function getContentEl(region) {
    let content = region.querySelector(".region-content");
    if (!content) {
        content = document.createElement("div");
        content.className = "region-content";
        region.appendChild(content);
    }
    return content;
}
export function bindRegion(el, store, topic, render, emptyMessage = "No data available") {
    const content = getContentEl(el);
    return store.subscribe(topic, (payload) => {
        if (payload.status === "error") {
            setRegionState(el, "error");
            content.innerHTML =
                `<div class="region-error"><p>${escapeHtml(payload.error ?? "An error occurred")}</p></div>`;
        }
        else if (!payload.data || payload.data.length === 0) {
            setRegionState(el, "empty");
            content.innerHTML =
                `<div class="region-empty"><p>${escapeHtml(emptyMessage)}</p></div>`;
        }
        else {
            setRegionState(el, "resolved");
            content.innerHTML = render(payload.data);
        }
    });
}
function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}
