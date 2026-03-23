import type { Store, Unsubscribe } from "../../state/store.types";
import type { TopicPayload } from "../../state/topics";
import type { RegionState, RenderFn } from "./region.types";
import { renderError, renderSimpleError } from "../renderers/error";

function setRegionState(el: HTMLElement, state: RegionState): void {
  el.dataset.state = state;
}

function getContentEl(region: HTMLElement): HTMLElement {
  let content = region.querySelector<HTMLElement>(".region-content");
  if (!content) {
    content = document.createElement("div");
    content.className = "region-content";
    region.appendChild(content);
  }
  return content;
}

export function bindRegion<T>(
  el: HTMLElement,
  store: Store,
  topic: string,
  render: RenderFn<T[]>,
  emptyMessage = "No data available",
): Unsubscribe {
  const content = getContentEl(el);

  return store.subscribe<TopicPayload<T[]>>(topic, (payload) => {
    if (payload.status === "error") {
      setRegionState(el, "error");
      if (payload.normalizedError) {
        content.innerHTML = renderError(payload.normalizedError);
      } else {
        content.innerHTML = renderSimpleError(
          payload.error ?? "An error occurred",
        );
      }
    } else if (!payload.data || payload.data.length === 0) {
      setRegionState(el, "empty");
      content.innerHTML =
        `<div class="region-empty"><p>${escapeHtml(emptyMessage)}</p></div>`;
    } else {
      setRegionState(el, "resolved");
      content.innerHTML = render(payload.data);
    }
  });
}

function escapeHtml(str: string): string {
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}
