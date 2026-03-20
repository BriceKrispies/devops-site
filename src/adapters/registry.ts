import type { Store } from "../state/store.types";
import type { DataAdapter } from "./adapter.interface";

export function registerAdapters(store: Store, adapters: DataAdapter[]): () => void {
  for (const adapter of adapters) {
    adapter.start(store);
  }

  return () => {
    for (const adapter of adapters) {
      adapter.stop();
    }
  };
}
