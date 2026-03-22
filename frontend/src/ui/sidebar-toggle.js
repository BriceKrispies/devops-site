"use strict";
const toggle = document.querySelector(".topbar-menu-toggle");
const sidebar = document.querySelector(".sidebar");
const backdrop = document.querySelector(".sidebar-backdrop");
function open() {
    sidebar?.classList.add("is-open");
    backdrop?.classList.add("is-open");
}
function close() {
    sidebar?.classList.remove("is-open");
    backdrop?.classList.remove("is-open");
}
toggle?.addEventListener("click", () => {
    sidebar?.classList.contains("is-open") ? close() : open();
});
backdrop?.addEventListener("click", close);
document.addEventListener("keydown", (e) => {
    if (e.key === "Escape")
        close();
});
