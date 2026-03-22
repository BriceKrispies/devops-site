export function registerAdapters(store, adapters) {
    for (const adapter of adapters) {
        adapter.start(store);
    }
    return () => {
        for (const adapter of adapters) {
            adapter.stop();
        }
    };
}
