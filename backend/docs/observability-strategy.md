# Observability Strategy

> Derived from [Backend Constitution](./backend-constitution.md) §7.

## Purpose

Define what every operation must emit, what every external call must emit, and how observability is tested. Observability is part of correctness — a capability is incomplete if it cannot be traced, logged, measured, and audited.

## Observability Pillars

### 1. Structured Logs

**Rule:** All logs must be structured (JSON or equivalent key-value). String-only log messages are forbidden for application behavior.

Every important log entry must include:

| Field | Required | Description |
|---|---|---|
| `operationName` | Yes | Name of the capability or operation |
| `correlationId` | Yes | Request/workflow correlation ID |
| `causationId` | If applicable | ID of the event that caused this operation |
| `result` | Yes | `success`, `failure`, `partial` |
| `latencyMs` | If applicable | Duration of the operation |
| `externalTarget` | If applicable | Name of external dependency called |
| `errorCode` | On failure | Classified error code from [Error Model](./error-model.md) |
| `severity` | Yes | `debug`, `info`, `warn`, `error`, `fatal` |
| `actor` | If applicable | Identity of the user or service |
| `tenantId` | If applicable | Scope/tenant identifier |

**Forbidden:**
- `console.log("something happened")` in application code
- Unstructured string logs for business operations
- Logging sensitive data (secrets, tokens, PII) — use redaction

### 2. Traces

**Rule:** Every external call and every top-level capability execution must emit spans.

| Span Type | Required Attributes |
|---|---|
| Capability span | `operationName`, `correlationId`, `result`, `latencyMs` |
| External call span | `externalTarget`, `operationName`, `result`, `latencyMs`, `retryCount` |
| Background job span | `jobName`, `correlationId`, `result`, `latencyMs` |

Spans must be nested: a capability span contains child spans for each external call.

### 3. Metrics

Every important capability must emit:

| Metric | Type | Labels |
|---|---|---|
| `capability.invocations` | Counter | `operationName`, `result` |
| `capability.latency` | Histogram | `operationName` |
| `capability.errors` | Counter | `operationName`, `errorCode` |
| `external.calls` | Counter | `externalTarget`, `result` |
| `external.latency` | Histogram | `externalTarget` |
| `external.retries` | Counter | `externalTarget` |

### 4. Audit Events

See [Audit & Admin Actions](./audit-and-admin-actions.md).

Every privileged or operationally meaningful action must emit an audit record. Audit events are distinct from logs — they are durable, queryable records of who did what and when.

## Operation Context Requirements

Every command, query, and background operation must carry an [Operation Context](./operation-context.md) that includes at minimum:

- `correlationId`
- `operationName`
- `actor` (where applicable)
- `tenantId` (where applicable)

Missing required context is a failure. See [Runtime Guards](./runtime-guards.md).

## Per-Capability Observability Requirements

When defining a new capability, the following observability artifacts are required:

| Artifact | Description |
|---|---|
| Capability span | Top-level trace span for the operation |
| Structured log on completion | Log entry with result, latency, error code if failed |
| Success/failure metric | Counter incremented on each invocation |
| Latency metric | Histogram recording duration |
| Audit event (if privileged) | Audit record for admin/mutation actions |
| External call spans (if applicable) | Child spans for each port call to external systems |

## Observability for External Dependency Calls

Every call through a port to an external system must emit:

1. A child trace span with `externalTarget` and `result`
2. A structured log on failure with `errorCode` and `externalTarget`
3. Metrics for call count, latency, and retry count

This telemetry should be implemented in the adapter layer, not in application logic.

## Testing Observability

Observability is not optional and must be asserted in tests.

**How to test:**
- Inject a fake/in-memory telemetry collector in tests
- After executing a use case, assert:
  - Expected spans were emitted with correct attributes
  - Expected structured log entries were recorded
  - Expected metrics were incremented
  - Expected audit events were emitted (for privileged actions)

**Example assertion patterns:**
```
// After executing a use case:
expect(telemetry.spans).toContainSpan({
  operationName: 'RerunJob',
  result: 'success',
})
expect(telemetry.logs).toContainEntry({
  operationName: 'RerunJob',
  correlationId: context.correlationId,
  result: 'success',
})
expect(telemetry.metrics.counter('capability.invocations')).toHaveBeenIncremented({
  operationName: 'RerunJob',
  result: 'success',
})
```

## Observability Acceptance Criteria

A new capability passes observability review when:

- [ ] Top-level span is emitted with correct operation name
- [ ] Structured log on completion includes all required fields
- [ ] Success/failure counter is incremented
- [ ] Latency histogram is recorded
- [ ] External calls emit child spans
- [ ] Failures include classified error code in log and span
- [ ] Privileged actions emit audit events
- [ ] Observability behavior is asserted in at least one test

## Enforcement

| Rule | Mechanism |
|---|---|
| No unstructured logs | Lint rule: forbid `console.log` in `src/` except `src/host/` bootstrap |
| Required span emission | Observability assertion tests |
| Required metrics | Observability assertion tests |
| Audit events for privileged actions | Observability assertion tests + PR review checklist |
| Correlation ID propagation | Runtime guard: reject operations with missing context |

See [CI Gates](./ci-gates.md), [Runtime Guards](./runtime-guards.md).
