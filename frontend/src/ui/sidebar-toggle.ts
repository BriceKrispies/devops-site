const toggle = document.querySelector<HTMLButtonElement>(".topbar-menu-toggle");
const sidebar = document.querySelector<HTMLElement>(".sidebar");
const backdrop = document.querySelector<HTMLElement>(".sidebar-backdrop");

function open(): void {
  sidebar?.classList.add("is-open");
  backdrop?.classList.add("is-open");
}

function close(): void {
  sidebar?.classList.remove("is-open");
  backdrop?.classList.remove("is-open");
}

toggle?.addEventListener("click", () => {
  sidebar?.classList.contains("is-open") ? close() : open();
});

backdrop?.addEventListener("click", close);

document.addEventListener("keydown", (e) => {
  if (e.key === "Escape") close();
});
