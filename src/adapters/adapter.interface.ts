import type { Store } from "../state/store.types";

export interface DataAdapter {
  start(store: Store): void;
  stop(): void;
}
