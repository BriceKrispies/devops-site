/**
 * Scheduling primitives for non-blocking work.
 *
 * All expensive or deferrable computation should route through these
 * helpers so the UI thread is never blocked by application logic.
 */
/** Defer a callback to the next microtask. */
export function defer(fn) {
    queueMicrotask(fn);
}
/** Schedule work during browser idle time. Falls back to setTimeout(0). */
export function scheduleIdle(fn, timeout) {
    const w = globalThis;
    if (typeof w.requestIdleCallback === "function") {
        return w.requestIdleCallback(fn, timeout !== undefined ? { timeout } : undefined);
    }
    return setTimeout(() => fn({ timeRemaining: () => 50 }), 0);
}
/** Cancel a scheduled idle callback. */
export function cancelIdle(id) {
    const w = globalThis;
    if (typeof w.cancelIdleCallback === "function") {
        w.cancelIdleCallback(id);
    }
    else {
        clearTimeout(id);
    }
}
/**
 * Process an array in chunks across microtasks.
 * Prevents long-running loops from blocking the UI.
 */
export function chunked(items, chunkSize, process, onComplete) {
    let offset = 0;
    function next() {
        const chunk = items.slice(offset, offset + chunkSize);
        if (chunk.length === 0) {
            onComplete?.();
            return;
        }
        process(chunk);
        offset += chunkSize;
        defer(next);
    }
    defer(next);
}
