/**
 * Tests for API client error normalization.
 * These are type-checked by tsc and can be run with any test runner.
 * They validate the normalizeApiError function which is the core
 * error normalization logic (does not require fetch/DOM).
 */
import { normalizeApiError } from "./api-client";
function assert(condition, message) {
    if (!condition)
        throw new Error(`Assertion failed: ${message}`);
}
function assertEqual(actual, expected, label) {
    if (actual !== expected)
        throw new Error(`${label}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
}
// --- normalizeApiError tests ---
function test_validation_error_normalizes_correctly() {
    const apiError = {
        code: "VALIDATION",
        message: "Name is required.",
        correlationId: "corr-001",
        fieldErrors: { name: "Required" },
    };
    const result = normalizeApiError(apiError, 400);
    assertEqual(result.kind, "validation", "kind");
    assertEqual(result.title, "Invalid Input", "title");
    assertEqual(result.message, "Name is required.", "message");
    assertEqual(result.correlationId, "corr-001", "correlationId");
    assertEqual(result.canRetry, false, "canRetry");
    assert(result.fieldErrors !== null, "fieldErrors should be present");
    assertEqual(result.fieldErrors["name"], "Required", "fieldErrors.name");
}
function test_not_found_error_normalizes_correctly() {
    const apiError = {
        code: "NOT_FOUND",
        message: "Service not found.",
        correlationId: "corr-002",
    };
    const result = normalizeApiError(apiError, 404);
    assertEqual(result.kind, "not-found", "kind");
    assertEqual(result.title, "Not Found", "title");
    assertEqual(result.canRetry, false, "canRetry");
    assertEqual(result.correlationId, "corr-002", "correlationId");
}
function test_unauthenticated_error_normalizes_to_permission() {
    const apiError = {
        code: "UNAUTHENTICATED",
        message: "Authentication required.",
        correlationId: "corr-003",
    };
    const result = normalizeApiError(apiError, 401);
    assertEqual(result.kind, "permission", "kind");
    assertEqual(result.title, "Access Denied", "title");
    assertEqual(result.canRetry, false, "canRetry");
}
function test_forbidden_error_normalizes_to_permission() {
    const apiError = {
        code: "FORBIDDEN",
        message: "Missing permission.",
        correlationId: "corr-004",
    };
    const result = normalizeApiError(apiError, 403);
    assertEqual(result.kind, "permission", "kind");
}
function test_dependency_unavailable_normalizes_to_unavailable() {
    const apiError = {
        code: "DEPENDENCY_UNAVAILABLE",
        message: "External service is down.",
        correlationId: "corr-005",
    };
    const result = normalizeApiError(apiError, 502);
    assertEqual(result.kind, "unavailable", "kind");
    assertEqual(result.title, "Service Unavailable", "title");
    assertEqual(result.canRetry, true, "canRetry");
}
function test_internal_error_normalizes_to_unexpected() {
    const apiError = {
        code: "INTERNAL_ERROR",
        message: "Something went wrong while processing your request.",
        correlationId: "corr-006",
    };
    const result = normalizeApiError(apiError, 500);
    assertEqual(result.kind, "unexpected", "kind");
    assertEqual(result.title, "Something Went Wrong", "title");
    assertEqual(result.canRetry, true, "canRetry");
    assertEqual(result.correlationId, "corr-006", "correlationId");
}
function test_conflict_error_normalizes_correctly() {
    const apiError = {
        code: "CONFLICT",
        message: "Resource already exists.",
        correlationId: "corr-007",
    };
    const result = normalizeApiError(apiError, 409);
    assertEqual(result.kind, "conflict", "kind");
    assertEqual(result.title, "Conflict", "title");
    assertEqual(result.canRetry, false, "canRetry");
}
function test_rate_limited_normalizes_correctly() {
    const apiError = {
        code: "RATE_LIMITED",
        message: "Too many requests.",
        correlationId: "corr-008",
    };
    const result = normalizeApiError(apiError, 429);
    assertEqual(result.kind, "rate-limited", "kind");
    assertEqual(result.canRetry, true, "canRetry");
}
function test_kill_switch_normalizes_to_unavailable() {
    const apiError = {
        code: "KILL_SWITCH_ACTIVE",
        message: "Capability disabled.",
        correlationId: "corr-009",
    };
    const result = normalizeApiError(apiError, 503);
    assertEqual(result.kind, "unavailable", "kind");
    assertEqual(result.canRetry, true, "canRetry");
}
function test_unknown_code_falls_back_to_http_status() {
    const apiError = {
        code: "SOMETHING_UNKNOWN",
        message: "Mystery error.",
        correlationId: "corr-010",
    };
    // 502 maps to "unavailable"
    const result = normalizeApiError(apiError, 502);
    assertEqual(result.kind, "unavailable", "kind");
}
function test_no_field_errors_results_in_null() {
    const apiError = {
        code: "VALIDATION",
        message: "Bad input.",
        correlationId: "corr-011",
    };
    const result = normalizeApiError(apiError, 400);
    assertEqual(result.fieldErrors, null, "fieldErrors should be null");
}
// Run all tests
const tests = [
    test_validation_error_normalizes_correctly,
    test_not_found_error_normalizes_correctly,
    test_unauthenticated_error_normalizes_to_permission,
    test_forbidden_error_normalizes_to_permission,
    test_dependency_unavailable_normalizes_to_unavailable,
    test_internal_error_normalizes_to_unexpected,
    test_conflict_error_normalizes_correctly,
    test_rate_limited_normalizes_correctly,
    test_kill_switch_normalizes_to_unavailable,
    test_unknown_code_falls_back_to_http_status,
    test_no_field_errors_results_in_null,
];
let passed = 0;
let failed = 0;
for (const test of tests) {
    try {
        test();
        passed++;
        console.log(`  ✓ ${test.name}`);
    }
    catch (err) {
        failed++;
        console.error(`  ✗ ${test.name}: ${err instanceof Error ? err.message : err}`);
    }
}
console.log(`\n${passed} passed, ${failed} failed`);
if (failed > 0)
    throw new Error(`${failed} test(s) failed`);
