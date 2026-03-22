import type { DataAdapter } from "../adapter.interface";
import type { Store } from "../../state/store.types";
import type { CapabilityShell } from "../../capabilities/types";
import type { TopicPayload } from "../../state/topics";
import { TOPICS } from "../../state/topics";
import { getAll } from "../../capabilities/catalog";

let timer: ReturnType<typeof setTimeout> | null = null;

export const mockCapabilitiesAdapter: DataAdapter = {
  start(store: Store) {
    timer = setTimeout(() => {
      const payload: TopicPayload<CapabilityShell[]> = {
        status: "ok",
        data: [...getAll()],
      };
      store.publish(TOPICS.CAPABILITIES, payload);
    }, 200);
  },
  stop() {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
    }
  },
};
