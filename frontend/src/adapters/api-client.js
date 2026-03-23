/** Header used for correlation ID propagation. */
const CORRELATION_HEADER = "x-correlation-id";
/**
 * Centralized fetch wrapper for backend API calls.
 * Parses and normalizes all failures into NormalizedError.
 * Handles network errors, timeouts, non-JSON responses, and structured backend errors.
 */
export async function apiFetch(url, options = {}) {
    let response;
    try {
        response = await fetch(url, {
            ...options,
            headers: {
                "Content-Type": "application/json",
                Accept: "application/json",
                ...options.headers,
            },
        });
    }
    catch (err) {
        return { ok: false, error: normalizeNetworkError(err) };
    }
    const correlationId = response.headers.get(CORRELATION_HEADER);
    if (response.ok) {
        try {
            const data = (await response.json());
            return { ok: true, data, correlationId };
        }
        catch {
            return {
                ok: false,
                error: {
                    title: "Invalid Response",
                    message: "The server returned an unreadable response.",
                    correlationId,
                    kind: "unexpected",
                    canRetry: true,
                    fieldErrors: null,
                },
            };
        }
    }
    // Non-success: try to parse as ApiErrorResponse
    return { ok: false, error: await normalizeErrorResponse(response, correlationId) };
}
/**
 * Parse a non-success response into a NormalizedError.
 * Handles both structured ApiErrorResponse bodies and unstructured failures.
 */
async function normalizeErrorResponse(response, correlationId) {
    let body = null;
    try {
        const contentType = response.headers.get("content-type") ?? "";
        if (contentType.includes("application/json")) {
            body = (await response.json());
        }
    }
    catch {
        // Could not parse JSON — fall through to status-based normalization
    }
    if (body && body.code && body.message) {
        return normalizeApiError(body, response.status);
    }
    // Fallback: status-based normalization for non-JSON or malformed responses
    return normalizeHttpStatus(response.status, correlationId);
}
/** Map a structured backend ApiErrorResponse into a NormalizedError. */
export function normalizeApiError(apiError, httpStatus) {
    const kind = mapCodeToKind(apiError.code, httpStatus);
    return {
        title: kindToTitle(kind),
        message: apiError.message,
        correlationId: apiError.correlationId ?? null,
        kind,
        canRetry: isRetryable(kind),
        fieldErrors: apiError.fieldErrors ?? null,
    };
}
/** Map backend error code + HTTP status to an ErrorKind. */
function mapCodeToKind(code, httpStatus) {
    switch (code) {
        case "VALIDATION":
            return "validation";
        case "UNAUTHENTICATED":
        case "FORBIDDEN":
            return "permission";
        case "NOT_FOUND":
            return "not-found";
        case "CONFLICT":
            return "conflict";
        case "DEPENDENCY_UNAVAILABLE":
        case "TIMEOUT":
        case "TRANSIENT_FAILURE":
        case "KILL_SWITCH_ACTIVE":
            return "unavailable";
        case "RATE_LIMITED":
            return "rate-limited";
        case "INTERNAL_ERROR":
        case "PERMANENT_FAILURE":
        default:
            // Fall back to HTTP status for unknown codes
            return httpStatusToKind(httpStatus);
    }
}
function httpStatusToKind(status) {
    if (status === 400)
        return "validation";
    if (status === 401 || status === 403)
        return "permission";
    if (status === 404)
        return "not-found";
    if (status === 409)
        return "conflict";
    if (status === 429)
        return "rate-limited";
    if (status === 502 || status === 503 || status === 504)
        return "unavailable";
    return "unexpected";
}
function kindToTitle(kind) {
    switch (kind) {
        case "validation":
            return "Invalid Input";
        case "permission":
            return "Access Denied";
        case "not-found":
            return "Not Found";
        case "conflict":
            return "Conflict";
        case "unavailable":
            return "Service Unavailable";
        case "rate-limited":
            return "Too Many Requests";
        case "unexpected":
            return "Something Went Wrong";
    }
}
function isRetryable(kind) {
    return kind === "unavailable" || kind === "rate-limited" || kind === "unexpected";
}
/** Normalize a network/fetch error (no response received). */
function normalizeNetworkError(err) {
    const isTimeout = err instanceof DOMException && err.name === "AbortError";
    return {
        title: isTimeout ? "Request Timed Out" : "Network Error",
        message: isTimeout
            ? "The request took too long to complete. Please try again."
            : "Unable to reach the server. Check your network connection and try again.",
        correlationId: null,
        kind: "unavailable",
        canRetry: true,
        fieldErrors: null,
    };
}
/** Normalize based on HTTP status when no structured body is available. */
function normalizeHttpStatus(status, correlationId) {
    const kind = httpStatusToKind(status);
    return {
        title: kindToTitle(kind),
        message: `The server returned an error (HTTP ${status}).`,
        correlationId,
        kind,
        canRetry: isRetryable(kind),
        fieldErrors: null,
    };
}
