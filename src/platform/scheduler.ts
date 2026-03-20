/**
 * Scheduling primitives for non-blocking work.
 *
 * All expensive or deferrable computation should route through these
 * helpers so the UI thread is never blocked by application logic.
 */

/** Defer a callback to the next microtask. */
export function defer(fn: () => void): void {
  queueMicrotask(fn);
}

/** Schedule work during browser idle time. Falls back to setTimeout(0). */
export function scheduleIdle(
  fn: (deadline: { timeRemaining(): number }) => void,
  timeout?: number,
): number {
  const w = globalThis as unknown as Record<string, unknown>;
  if (typeof w.requestIdleCallback === "function") {
    return (w.requestIdleCallback as (cb: typeof fn, opts?: { timeout: number }) => number)(
      fn,
      timeout !== undefined ? { timeout } : undefined,
    );
  }
  return setTimeout(() => fn({ timeRemaining: () => 50 }), 0) as unknown as number;
}

/** Cancel a scheduled idle callback. */
export function cancelIdle(id: number): void {
  const w = globalThis as unknown as Record<string, unknown>;
  if (typeof w.cancelIdleCallback === "function") {
    (w.cancelIdleCallback as (id: number) => void)(id);
  } else {
    clearTimeout(id);
  }
}

/**
 * Process an array in chunks across microtasks.
 * Prevents long-running loops from blocking the UI.
 */
export function chunked<T>(
  items: T[],
  chunkSize: number,
  process: (chunk: T[]) => void,
  onComplete?: () => void,
): void {
  let offset = 0;

  function next(): void {
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
