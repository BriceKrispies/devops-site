import type { Store, Unsubscribe } from "./store.types";

type Callback = (data: unknown) => void;

export function createStore(): Store {
  const cache = new Map<string, unknown>();
  const subscribers = new Map<string, Set<Callback>>();

  return {
    publish<T>(topic: string, data: T): void {
      cache.set(topic, data);
      const subs = subscribers.get(topic);
      if (subs) {
        for (const cb of subs) { cb(data); }
      }
    },

    subscribe<T>(topic: string, callback: (data: T) => void): Unsubscribe {
      let subs = subscribers.get(topic);
      if (!subs) { subs = new Set(); subscribers.set(topic, subs); }
      const cb = callback as Callback;
      subs.add(cb);

      const current = cache.get(topic);
      if (current !== undefined) { cb(current); }

      return () => { subs!.delete(cb); };
    },

    get<T>(topic: string): T | undefined {
      return cache.get(topic) as T | undefined;
    },
  };
}
