# Roadmap

> Phased adoption plan for operationalizing the [Backend Constitution](./backend-constitution.md).

## Principle

Mechanical enforcement comes first. Culture follows tooling, not the other way around.

## Day 0 — Must Exist Before First Capability

These are prerequisites. No application code is written until these are in place.

| Item | Artifact | Status |
|---|---|---|
| Project directory structure | `src/domain/`, `src/application/`, `src/adapters/`, `src/host/`, `src/tests/` | |
| Architecture test suite (dependency direction) | `tests/architecture/dependency-direction.arch.spec.ts` | |
| Forbidden import lint rules | ESLint config or equivalent: vendor SDKs in domain/app, `console.log`, `Date.now()` in domain | |
| Standard result type | `src/application/results/Result.ts` | |
| Standard error taxonomy | `src/application/errors/AppError.ts`, `ErrorCode` enum | |
| Operation context model | `src/application/context/OperationContext.ts` | |
| Structured logger interface | `src/application/ports/LoggerPort.ts` | |
| Base spec test template | `tests/helpers/spec-template.ts` | |
| Base contract test template | `tests/helpers/contract-template.ts` | |
| CI pipeline: build + type check + lint + architecture tests | `.github/workflows/ci.yml` or equivalent | |
| Documentation set | `docs/` (this set) | |

## Week 1 — First Capabilities with Full Compliance

| Item | Artifact | Status |
|---|---|---|
| First capability implemented with full scaffold | Use case + domain spec + app behavior test + observability | |
| Capability scaffold generator | `scripts/scaffold-capability.ts` or equivalent | |
| Port scaffold generator | `scripts/scaffold-port.ts` | |
| Adapter scaffold generator | `scripts/scaffold-adapter.ts` | |
| Telemetry port + in-memory adapter for tests | `TelemetryPort`, `InMemoryTelemetryAdapter` | |
| Audit port + in-memory adapter for tests | `AuditPort`, `InMemoryAuditAdapter` | |
| Clock port (determinism) | `ClockPort`, `SystemClockAdapter`, `FixedClockAdapter` (test) | |
| First port contract test suite | For the first external dependency | |
| First adapter passing contract suite | For the first external dependency | |
| Startup validation (config + port bindings) | Host composition root | |
| Request-time context guard (middleware) | Correlation ID + operation name validation | |

## First Month — Full Enforcement Pipeline

| Item | Artifact | Status |
|---|---|---|
| CI gates for all test layers | Domain, application, contracts, adapters, regression, architecture, observability | |
| CI gate: new capability must have spec tests | Structural check in CI | |
| CI gate: new port must have contract tests | Structural check in CI | |
| CI gate: new adapter must run contract suite | Structural check in CI | |
| CI gate: public contract changes require updated fixtures | Fixture drift detection | |
| Observability assertion test helpers | `tests/helpers/telemetry-assertions.ts` | |
| Exception registry | `docs/exception-registry.md` with automated expiry check | |
| Multiple adapters certified | AWS, Jira, GitHub, or other initial integrations | |
| Endpoint/job scaffold generator | `scripts/scaffold-endpoint.ts`, `scripts/scaffold-job.ts` | |
| Regression test workflow established | At least one regression test exists | |

## Later Hardening (Month 2+)

| Item | Artifact | Status |
|---|---|---|
| Mutation testing for critical domain logic | Stryker or equivalent configured for `src/domain/` | |
| PR policy checks (automated) | Bot or CI step checking definition of done | |
| Static rules for anti-pattern detection | Additional lint rules for business logic in host, untyped config, etc. | |
| AI prompt templates tied to constitution | Standard prompts referencing scaffolds and docs | |
| Adapter certification workflow | Automated checklist verification in CI | |
| Audit event retention and querying | Production audit store with search capability | |
| Observability dashboards | Grafana dashboards for capability metrics, error rates, latency | |
| Exception expiry automation | Scheduled CI job or bot that flags expired exceptions | |
| Cross-team documentation review | Validate docs remain accurate and complete | |

## Priority Order

If resources are constrained, prioritize in this order:

1. **Architecture tests** — prevent structural violations from day 0
2. **Standard result/error types** — establish the error model foundation
3. **Operation context** — enable tracing and observability
4. **Scaffolding** — ensure every new artifact has correct structure
5. **CI gates** — make enforcement automatic
6. **Contract tests** — certify adapters
7. **Observability assertions** — prove telemetry works
8. **Mutation testing** — harden critical logic
