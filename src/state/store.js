export function createStore() {
    const cache = new Map();
    const subscribers = new Map();
    return {
        publish(topic, data) {
            cache.set(topic, data);
            const subs = subscribers.get(topic);
            if (subs) {
                for (const cb of subs) {
                    cb(data);
                }
            }
        },
        subscribe(topic, callback) {
            let subs = subscribers.get(topic);
            if (!subs) {
                subs = new Set();
                subscribers.set(topic, subs);
            }
            const cb = callback;
            subs.add(cb);
            const current = cache.get(topic);
            if (current !== undefined) {
                cb(current);
            }
            return () => { subs.delete(cb); };
        },
        get(topic) {
            return cache.get(topic);
        },
    };
}
