import { TOPICS } from "../state/topics";
import { defer } from "../platform/scheduler";
function matchesQuery(event, query) {
    const q = query.toLowerCase();
    if (event.summary.toLowerCase().includes(q))
        return true;
    if (event.type.toLowerCase().includes(q))
        return true;
    if (event.source.toLowerCase().includes(q))
        return true;
    for (const val of Object.values(event.entityIds)) {
        if (val.toLowerCase().includes(q))
            return true;
    }
    for (const val of Object.values(event.metadata)) {
        if (val.toLowerCase().includes(q))
            return true;
    }
    return false;
}
/**
 * Effect: subscribes to TRACE_QUERY, reads raw data from TRACE_DATA,
 * filters/sorts off the UI thread via defer, publishes to TRACE_RESULTS.
 */
export function createTraceSearchEffect(store) {
    return store.subscribe(TOPICS.TRACE_QUERY, (query) => {
        const trimmed = query.trim();
        if (trimmed.length === 0)
            return;
        defer(() => {
            const dataPayload = store.get(TOPICS.TRACE_DATA);
            const events = dataPayload?.data ?? [];
            const results = events.filter((e) => matchesQuery(e, trimmed));
            results.sort((a, b) => a.timestamp.localeCompare(b.timestamp));
            const payload = { status: "ok", data: results };
            store.publish(TOPICS.TRACE_RESULTS, payload);
        });
    });
}
