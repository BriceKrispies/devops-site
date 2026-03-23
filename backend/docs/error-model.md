# Error Model

> Derived from [Backend Constitution](./backend-constitution.md) §9.

## Purpose

Define the backend's error taxonomy, typed error categories, mapping rules from vendor failures, and how errors appear in logs, traces, metrics, and API responses.

## Principles

1. Errors must be explicit, typed, and classified.
2. Business outcomes must not rely on thrown exceptions as normal control flow.
3. Every capability must classify errors into stable categories.
4. Errors must be observable (logged, traced, metered).

## Error Taxonomy

| Category | Code | Severity | Description | Example |
|---|---|---|---|---|
| Validation | `VALIDATION` | `warn` | Input fails schema or business validation | Missing required field, invalid date range |
| Authorization | `AUTHORIZATION` | `warn` | Caller lacks permission | User not in required role |
| Not Found | `NOT_FOUND` | `info` | Requested resource does not exist | Job ID not found |
| Conflict | `CONFLICT` | `warn` | Operation conflicts with current state | Duplicate submission, version mismatch |
| Rate Limited | `RATE_LIMITED` | `warn` | Caller exceeded rate limit | Too many API calls |
| Dependency Unavailable | `DEPENDENCY_UNAVAILABLE` | `error` | External system cannot be reached | Jira API unreachable |
| Timeout | `TIMEOUT` | `error` | Operation or dependency call exceeded deadline | AWS call timed out |
| Transient Dependency Failure | `TRANSIENT_FAILURE` | `error` | External system returned retriable error | 503 from Grafana |
| Permanent Dependency Failure | `PERMANENT_FAILURE` | `error` | External system returned non-retriable error | 400 from GitHub API |
| Internal Invariant Violation | `INVARIANT_VIOLATION` | `fatal` | A condition that should be impossible was detected | Null entity after successful save |

## Result Type

All use cases return a result type, never thrown exceptions for business flow.

```typescript
// Pseudocode — adapt to chosen language/framework
type Result<T, E extends AppError> =
  | { ok: true; value: T }
  | { ok: false; error: E }

interface AppError {
  code: ErrorCode        // From taxonomy above
  message: string        // Human-readable, non-sensitive
  severity: Severity     // debug | info | warn | error | fatal
  operationName: string  // Which capability produced this error
  correlationId: string  // From operation context
  dependency?: string    // Which external system, if applicable
  cause?: unknown        // Original error for debugging, not exposed externally
}
```

## Vendor Error Mapping

Every adapter must map vendor-specific errors into the internal taxonomy.

| Vendor Signal | Maps To |
|---|---|
| HTTP 400 | `VALIDATION` or `PERMANENT_FAILURE` (context-dependent) |
| HTTP 401/403 | `AUTHORIZATION` |
| HTTP 404 | `NOT_FOUND` |
| HTTP 409 | `CONFLICT` |
| HTTP 429 | `RATE_LIMITED` |
| HTTP 500 | `TRANSIENT_FAILURE` |
| HTTP 502/503/504 | `DEPENDENCY_UNAVAILABLE` or `TRANSIENT_FAILURE` |
| Connection refused/timeout | `DEPENDENCY_UNAVAILABLE` or `TIMEOUT` |
| SDK-specific exception | Classify into nearest taxonomy category |

**Rules:**
- Adapters must never propagate raw vendor exceptions to Application or Domain
- Adapters must never expose vendor-specific error types in port interfaces
- Mapping must be explicit, not a catch-all

## Error Appearance by Context

### In Logs

```json
{
  "level": "error",
  "operationName": "RerunJob",
  "correlationId": "abc-123",
  "errorCode": "DEPENDENCY_UNAVAILABLE",
  "message": "AWS Batch API unreachable",
  "dependency": "aws-batch",
  "latencyMs": 3012
}
```

### In Traces

Failed spans include:
- `error: true`
- `errorCode`: classified code
- `errorMessage`: non-sensitive message

### In Metrics

- `capability.errors` counter incremented with labels `operationName`, `errorCode`
- `external.calls` counter with `result: failure`

### In API Responses — Standardized Error Contract

All non-success API responses use the `ApiErrorResponse` contract:

```json
{
  "code": "NOT_FOUND",
  "message": "Job with ID job-456 not found",
  "correlationId": "a1b2c3d4e5f6"
}
```

For validation errors, field-level detail may be included:

```json
{
  "code": "VALIDATION",
  "message": "Input validation failed.",
  "correlationId": "a1b2c3d4e5f6",
  "fieldErrors": {
    "serviceId": "Must not be empty",
    "from": "Must be before 'to' date"
  }
}
```

**Contract fields:**

| Field | Type | Required | Description |
|---|---|---|---|
| `code` | string | yes | Stable machine-readable error code (SCREAMING_SNAKE_CASE) |
| `message` | string | yes | Safe user-facing message — never contains stack traces or internal details |
| `correlationId` | string | yes | Request correlation ID for cross-referencing backend logs |
| `details` | string | no | Optional safe details when additional context is appropriate |
| `fieldErrors` | object | no | Field-level validation errors: `{ fieldName: message }` |

**Stable error codes:**

| Code | HTTP Status | Description |
|---|---|---|
| `VALIDATION` | 400 | Input fails schema or business validation |
| `UNAUTHENTICATED` | 401 | No authenticated actor present |
| `FORBIDDEN` | 403 | Authenticated but missing required permission |
| `NOT_FOUND` | 404 | Requested resource does not exist |
| `CONFLICT` | 409 | Operation conflicts with current state |
| `RATE_LIMITED` | 429 | Caller exceeded rate limit |
| `DEPENDENCY_UNAVAILABLE` | 502 | External system unreachable |
| `TIMEOUT` | 502 | Operation or dependency exceeded deadline |
| `TRANSIENT_FAILURE` | 502 | External system returned retriable error |
| `PERMANENT_FAILURE` | 502 | External system returned non-retriable error |
| `INTERNAL_ERROR` | 500/502 | Unhandled exception or invariant violation |
| `KILL_SWITCH_ACTIVE` | 503 | Capability disabled via kill switch |

## Correlation ID

Every HTTP request carries a correlation ID:

1. The `OperationContextMiddleware` extracts the `X-Correlation-Id` request header or generates a new UUID.
2. The ID is stored in `OperationContext.CorrelationId`.
3. The ID is returned in the `X-Correlation-Id` response header on **every** response.
4. The ID is included in the `correlationId` field of **every** error response body.
5. The ID is included in **every** structured log entry for the request.

This makes it possible to trace any frontend-visible error back to the corresponding backend log entries.

## Global Exception Handler

The `ExceptionHandlerMiddleware` catches all unhandled exceptions at the top of the middleware pipeline:

- Logs rich context: exception type, stack trace, route, actor, correlation ID
- Returns a sanitized `INTERNAL_ERROR` response — never exposes stack traces or internal messages
- Handles client disconnection (`OperationCanceledException`) gracefully with HTTP 499

### Authorization Error HTTP Mapping (Constitution §14)

| Error Factory | HTTP Status | When |
|---|---|---|
| `AppError.Unauthenticated()` | 401 | No authenticated actor present |
| `AppError.Forbidden()` | 403 | Authenticated but missing required permission |

Both use `ErrorCode.Authorization`. The host `ResultMapper` distinguishes 401 vs 403 by the stable message prefix from the factory methods.

**Rules for API responses:**
- Never expose stack traces
- Never expose internal implementation details
- Never expose `cause` (internal debugging info)
- Use stable error codes from the taxonomy
- Messages should be actionable for the caller

## Forbidden Patterns

| Pattern | Why |
|---|---|
| `throw new Error("something")` for business outcomes | Untyped, unclassified, invisible to metrics |
| Catch-all `catch (e) { return generic error }` | Loses classification; all errors look the same |
| Returning HTTP status codes from Application layer | Application must not know about transport |
| Logging `error.stack` to API response | Security risk, information leakage |
| Inventing error codes outside the taxonomy | Inconsistent metrics, unrecognizable codes |

## Adding a New Error Category

If a new category is genuinely needed:

1. Propose it with rationale
2. Add it to this document (taxonomy table + stable code table)
3. Add the value to the `ErrorCode` enum in `Application/Errors/ErrorCode.cs`
4. Add a factory method on `AppError` in `Application/Results/AppError.cs`
5. Add the stable string code mapping in `ResultMapper.ToErrorCode()` in `Host/Routes/ResultMapper.cs`
6. Add the HTTP status mapping in `ResultMapper.ToStatusCode()`
7. Update metric labels and log schema
8. Update the frontend `mapCodeToKind()` in `adapters/api-client.ts`

## Enforcement

| Rule | Mechanism |
|---|---|
| Use cases return Result types, not thrown exceptions | Lint rule + code review |
| Adapters map vendor errors to taxonomy | Adapter contract tests |
| Errors include required fields | Type system + unit tests |
| No new codes outside taxonomy | Type system (enum/union) |
| Errors appear in logs and metrics | Observability assertion tests |

See [Adapter Certification](./adapter-certification.md), [CI Gates](./ci-gates.md).
