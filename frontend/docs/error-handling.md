# Error Handling

> Frontend error normalization, rendering, and correlation ID display.

## Principles

1. Never render raw `fetch` exceptions or arbitrary backend payloads to users.
2. All API failures are normalized into a `NormalizedError` before reaching UI code.
3. Correlation IDs are displayed for non-trivial backend failures so users can reference them in support requests.
4. Pages degrade gracefully — error banners replace region content without collapsing the page shell.
5. Validation errors show actionable field-level messages when available.

## Error Types

### `ApiErrorResponse` (`types/api-error.ts`)

Matches the backend's standardized error contract:

```typescript
interface ApiErrorResponse {
  code: string;           // e.g. "VALIDATION", "NOT_FOUND", "INTERNAL_ERROR"
  message: string;        // Safe user-facing message
  correlationId: string;  // Request correlation ID
  details?: string;       // Optional safe details
  fieldErrors?: Record<string, string>;  // Field-level validation errors
}
```

### `NormalizedError` (`types/api-error.ts`)

UI-safe error model that separates raw transport failures from display:

```typescript
interface NormalizedError {
  title: string;           // e.g. "Invalid Input", "Service Unavailable"
  message: string;         // User-facing description
  correlationId: string | null;  // null for network errors (no backend response)
  kind: ErrorKind;         // "validation" | "permission" | "not-found" | "conflict" | "unavailable" | "rate-limited" | "unexpected"
  canRetry: boolean;       // true for unavailable, rate-limited, unexpected
  fieldErrors: Record<string, string> | null;
}
```

### `ErrorKind`

| Kind | Backend Codes | Retryable | Display |
|---|---|---|---|
| `validation` | `VALIDATION` | no | Field-level messages when available |
| `permission` | `UNAUTHENTICATED`, `FORBIDDEN` | no | Access denied message |
| `not-found` | `NOT_FOUND` | no | Resource not found message |
| `conflict` | `CONFLICT` | no | State conflict message |
| `unavailable` | `DEPENDENCY_UNAVAILABLE`, `TIMEOUT`, `TRANSIENT_FAILURE`, `KILL_SWITCH_ACTIVE` | yes | Service unavailable + retry |
| `rate-limited` | `RATE_LIMITED` | yes | Too many requests + retry |
| `unexpected` | `INTERNAL_ERROR`, `PERMANENT_FAILURE`, unknown | yes | Generic error + correlation ID + retry |

## API Client (`adapters/api-client.ts`)

The centralized `apiFetch<T>()` function handles all backend communication:

```typescript
const result = await apiFetch<ServiceHealthResponse>("/api/health/svc-1");
if (result.ok) {
  // result.data is typed as ServiceHealthResponse
  // result.correlationId is the response correlation ID
} else {
  // result.error is a NormalizedError — safe for UI display
}
```

The client normalizes **all** failure modes:
- Structured backend errors (JSON `ApiErrorResponse` bodies)
- Non-JSON error responses (falls back to HTTP status mapping)
- Network errors (no response received)
- Timeouts (`AbortError`)
- Malformed JSON responses

## Error Rendering (`ui/renderers/error.ts`)

The `renderError(error)` function generates an error banner HTML string:

```typescript
import { renderError } from "../renderers/error";

const html = renderError(normalizedError);
// Returns an `.error-banner` div with:
//   - title
//   - safe message
//   - correlation ID (when present)
//   - field error list (for validation errors)
//   - retry button (when canRetry is true)
```

### Error banner CSS classes

| Class | Purpose |
|---|---|
| `.error-banner` | Base error banner container |
| `.error-banner--{kind}` | Kind-specific styling (border color) |
| `.error-banner-title` | Error title text |
| `.error-banner-message` | Error message text |
| `.error-correlation` | Correlation ID display (monospace) |
| `.error-field-list` | Validation field error list |
| `.error-retry-btn` | Retry button |

## Region Integration

The `TopicPayload<T>` type supports rich errors:

```typescript
interface TopicPayload<T> {
  status: "ok" | "error";
  data?: T;
  error?: string;                    // Legacy: plain error message
  normalizedError?: NormalizedError; // Rich: normalized error for banner rendering
}
```

When a region receives an error payload:
- If `normalizedError` is present, renders an error banner with correlation ID
- Falls back to the plain `error` string with simple rendering

## How to Consume Errors in a Real Adapter

```typescript
import { apiFetch } from "../api-client";
import { TOPICS, type TopicPayload } from "../../state/topics";

async function fetchData(store: Store): Promise<void> {
  const result = await apiFetch<MyResponse>("/api/my-endpoint");

  if (result.ok) {
    store.publish(TOPICS.MY_TOPIC, { status: "ok", data: result.data });
  } else {
    store.publish(TOPICS.MY_TOPIC, {
      status: "error",
      error: result.error.message,
      normalizedError: result.error,
    });
  }
}
```

## When Details Are Safe to Expose

| Scenario | Safe? | Guidance |
|---|---|---|
| Validation field errors from backend | Yes | Backend only includes safe field names and messages |
| Backend error `message` field | Yes | Backend sanitizes all messages before including in response |
| Backend error `code` field | Yes | Stable machine-readable codes, not sensitive |
| Correlation ID | Yes | Opaque identifier, no sensitive info |
| Raw `fetch` error messages | No | May contain internal URLs or network details |
| Stack traces | No | Never exposed by backend, never displayed in UI |
| Backend `details` field | Usually | Only populated when backend deems it safe |

## Testing

Tests in `adapters/api-client.test.ts` validate:
- All backend error codes map to correct `ErrorKind`
- Correlation IDs propagate through normalization
- Field errors are preserved for validation failures
- Retryability flags are correct per kind
- Null field errors for non-validation errors
