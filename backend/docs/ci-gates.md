# CI Gates

> Derived from [Backend Constitution](./backend-constitution.md) §13.4.

## Purpose

Define the required CI checks for merge. A PR must not merge if any gate fails.

## Merge Blockers

The following checks must all pass for a PR to merge:

| Gate | What It Checks | Failure Means |
|---|---|---|
| **Architecture tests** | Dependency directions, namespace boundaries, layering rules | Code violates system shape |
| **Static analyzers** | Forbidden imports, banned APIs, structured logging rules | Code uses prohibited patterns |
| **Domain spec tests** | Business rules, invariants, state transitions | Business logic is broken |
| **Application behavior tests** | Use case orchestration, error handling | Use case is broken |
| **Port contract tests** | Port behavioral contracts | Port contract is violated |
| **Adapter tests** | Adapter passes contract suite + vendor specifics | Adapter is broken or uncertified |
| **Observability assertions** | Required telemetry is emitted | Capability is not observable |
| **Security metadata tests** | Every handler has CapabilityDescriptor, all registered, privileged require audit | Security metadata missing or invalid |
| **Startup validation** | CapabilityRegistry validates all descriptors at boot | Missing or inconsistent auth declarations |
| **Lint/format** | Code style consistency | Code style violation |
| **Type check** | Type system passes | Type error |
| **Build** | Project compiles/builds | Build failure |

## Gate Details

### Architecture Tests

- Import graph matches allowed dependency matrix
- No forbidden cross-layer references
- No vendor SDKs in Domain or Application
- No business logic in Host layer

See [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md).

### Static Analyzers

- No `process.env` in Domain or Application
- No `Date.now()` or `new Date()` in Domain
- No `Math.random()` in Domain
- No `console.log` in `src/` (except Host bootstrap)
- No untyped config access
- Structured logging patterns enforced

### Test Suites

All test suites must pass:

| Suite | Directory | Required |
|---|---|---|
| Domain specs | `tests/domain/` | Yes |
| Application behaviors | `tests/application/` | Yes |
| Port contracts | `tests/contracts/` | Yes |
| Adapter tests | `tests/adapters/` | Yes |
| Regression tests | `tests/regression/` | Yes |
| Architecture tests | `tests/architecture/` | Yes |
| Observability assertions | Within application tests | Yes |

### New Capability Checks

When a PR introduces a new capability:

- [ ] Use case handler file exists
- [ ] Domain spec test file exists and is non-empty
- [ ] Application behavior test file exists and is non-empty
- [ ] Port contract test exists for any new port
- [ ] Observability assertions exist
- [ ] Capability is registered in DI composition

### Public Contract Change Checks

When a PR modifies a public contract (port interface, API endpoint, event schema):

- [ ] Contract test suite is updated
- [ ] Adapter tests still pass
- [ ] API response fixtures are updated (if applicable)
- [ ] Breaking change is documented

### Exception Tracking

If the PR includes a temporary exception to constitution rules:

- [ ] Exception is documented per [Exception Policy](./exception-policy.md)
- [ ] Exception has an owner
- [ ] Exception has an expiry date
- [ ] Exception is tracked in the exception registry

## CI Pipeline Stages

```
Stage 1: Build + Type Check + Lint
  └── Fail fast on compilation or formatting errors

Stage 2: Static Analysis + Architecture Tests
  └── Fail fast on structural violations

Stage 3: Domain Spec Tests
  └── Fail fast on business logic failures

Stage 4: Application Behavior Tests + Observability Assertions
  └── Fail fast on use case failures

Stage 5: Port Contract Tests + Adapter Tests
  └── Fail fast on integration failures

Stage 6: Regression Tests
  └── Catch regressions
```

Stages are ordered by speed and blast radius. Fast, foundational checks run first.

## Prohibited CI Patterns

| Pattern | Why |
|---|---|
| Skipping architecture tests | Constitution violations slip in |
| Allowing merge with failing contract tests | Broken adapters reach production |
| Retry-until-pass for flaky tests | Masks real failures; fix the flakiness |
| Manual gate override without exception record | Bypasses enforcement without accountability |

## Enforcement

This document defines CI configuration requirements. The CI pipeline itself must be treated as infrastructure and reviewed with the same rigor as application code.
