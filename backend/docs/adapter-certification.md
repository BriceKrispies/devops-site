# Adapter Certification

> Derived from [Backend Constitution](./backend-constitution.md) §6.2C–D, §10.

## Purpose

Define what it means for an adapter to be accepted into the system. An adapter is not a first-class citizen until it passes certification.

**This is a MANDATORY rule. No adapter may be merged without passing certification. The scaffold generates certification test stubs that must all be resolved before the adapter ships.**

## Reference Implementation

The Jira adapter (`src/DevOpsSite.Adapters/Jira/`) is the certified reference implementation. Study it before building a new adapter:

- `JiraWorkItemAdapter.cs` — Full error mapping, telemetry, internal DTOs
- `JiraConfig.cs` — Typed config with validation
- `FakeWorkItemAdapter.cs` — Fake for tests
- `tests/DevOpsSite.Adapters.Tests/Jira/JiraWorkItemAdapterTests.cs` — Certification tests with HTTP stubs
- `tests/DevOpsSite.Adapters.Tests/Jira/FakeWorkItemAdapterTests.cs` — Contract suite runner

## Certification Requirements

### 1. Port Contract Tests

The adapter must pass the full [port contract test suite](./port-contract-template.md) for every port it implements.

- [ ] Adapter is tested by importing and running the contract suite
- [ ] All normal behavior tests pass
- [ ] All edge case tests pass
- [ ] All failure behavior tests pass
- [ ] Timeout tests pass
- [ ] Retry tests pass
- [ ] Cancellation tests pass (if port defines cancellation)

### 2. Error Mapping

The adapter must map all vendor-specific errors into the internal [error taxonomy](./error-model.md).

- [ ] HTTP status codes are mapped per the error model mapping table
- [ ] SDK-specific exceptions are classified into the nearest taxonomy category
- [ ] No raw vendor exceptions propagate through the port interface
- [ ] Error mapping is tested explicitly (given vendor error X, adapter returns AppError Y)

### 3. Telemetry

The adapter must emit telemetry as defined in [Observability Strategy](./observability-strategy.md).

- [ ] Emits a trace span for every external call
- [ ] Span includes `externalTarget`, `result`, `latencyMs`
- [ ] Emits structured log on failure with `errorCode`, `externalTarget`, `correlationId`
- [ ] Increments `external.calls` counter with correct labels
- [ ] Increments `external.latency` histogram
- [ ] Increments `external.retries` counter (if retries occur)
- [ ] Telemetry emission is tested with a fake telemetry collector

### 4. Timeout and Retry Behavior

- [ ] Adapter respects configured timeout (does not hang indefinitely)
- [ ] Adapter retries on transient failures (if retry policy is configured)
- [ ] Adapter does not retry on permanent failures
- [ ] Adapter respects max retry count
- [ ] Timeout and retry behavior is configurable, not hardcoded
- [ ] Backoff strategy is applied (e.g., exponential backoff with jitter)

### 5. Configuration

- [ ] Adapter configuration is typed (not arbitrary string lookups)
- [ ] Configuration is validated at startup (fail-fast on invalid values)
- [ ] Secrets are not logged or exposed in error messages
- [ ] Default values are sensible and documented

See [Configuration Strategy](./configuration-strategy.md).

### 6. No Vendor Leakage

- [ ] Port interface methods accept/return only domain and application types
- [ ] No vendor SDK types appear in the adapter's public API
- [ ] Vendor-specific request/response types are internal to the adapter module
- [ ] No vendor SDK re-exports from the adapter

### 7. Serialization Boundaries

- [ ] Adapter correctly serializes domain types to vendor format
- [ ] Adapter correctly deserializes vendor responses to domain types
- [ ] Serialization edge cases are tested (nulls, empty collections, large payloads)
- [ ] Date/time serialization is tested (timezone handling, format compliance)

## Certification Checklist

Use this checklist in PRs that introduce or modify an adapter:

```markdown
### Adapter Certification: {AdapterName} for {PortName}

- [ ] Passes full port contract suite
- [ ] Error mapping tested for all vendor error types
- [ ] Telemetry: spans, logs, metrics emitted and tested
- [ ] Timeout behavior configured and tested
- [ ] Retry behavior configured and tested
- [ ] Configuration is typed and validated at startup
- [ ] No vendor types leak through port interface
- [ ] Serialization boundaries tested
- [ ] Secrets are not logged or exposed
- [ ] Adapter registered in DI composition
```

## Prohibited Patterns

| Pattern | Why |
|---|---|
| Adapter returns vendor SDK types | Leaks vendor coupling into core layers |
| Adapter throws raw vendor exceptions | Bypasses error taxonomy |
| Adapter hardcodes retry count or timeout | Not configurable; cannot be tuned per environment |
| Adapter logs secrets or auth tokens | Security violation |
| Adapter contains business logic | Business logic belongs in Domain or Application |
| Adapter is tested only with mocks of the vendor SDK | Must also run contract suite; mock-only tests miss real behavior |

## Enforcement

| Rule | Mechanism |
|---|---|
| Contract suite passes | CI gate: adapter test must import and run contract suite |
| No vendor leakage | Architecture test: adapter public API does not export vendor types |
| Telemetry emission | Observability assertion tests |
| Config validated | Startup validation + tests |

See [CI Gates](./ci-gates.md), [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md).
