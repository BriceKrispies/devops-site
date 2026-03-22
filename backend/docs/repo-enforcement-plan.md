# Repo Enforcement Plan

> Translates the [Backend Constitution](./backend-constitution.md) into concrete enforcement mechanisms.

## Purpose

Map every constitutional rule to a specific enforcement mechanism, define failure modes, and organize by enforcement layer with phased rollout.

## Enforcement by Layer

### Layer 1: Static Analysis (Lint Rules)

| Rule | Intent | Mechanism | Failure Mode | Priority |
|---|---|---|---|---|
| No vendor SDKs in Domain | Domain purity | Lint rule: deny-list vendor packages in `src/domain/` imports | Lint error, CI blocks | Day 0 |
| No vendor SDKs in Application | Application isolation | Lint rule: deny-list vendor packages in `src/application/` imports | Lint error, CI blocks | Day 0 |
| No `Date.now()`/`new Date()` in Domain | Deterministic core | ESLint `no-restricted-syntax` for Date calls in `src/domain/` | Lint error, CI blocks | Day 0 |
| No `Math.random()` in Domain | Deterministic core | ESLint `no-restricted-syntax` in `src/domain/` | Lint error, CI blocks | Day 0 |
| No `process.env` in Domain/Application | Config isolation | ESLint `no-restricted-globals` in `src/domain/`, `src/application/` | Lint error, CI blocks | Day 0 |
| No `console.log` in src/ | Structured logging | ESLint `no-console` in `src/` (allow in `src/host/` bootstrap) | Lint error, CI blocks | Day 0 |
| No `fs`/`child_process` in Domain | Domain purity | ESLint `no-restricted-imports` in `src/domain/` | Lint error, CI blocks | Day 0 |
| No framework decorators in Domain | Domain purity | Lint rule for decorator patterns in `src/domain/` | Lint error, CI blocks | Day 0 |
| No bare `throw` in use cases | Error model compliance | Custom lint rule for `src/application/use-cases/` | Lint error, CI blocks | Week 1 |
| Structured logging required fields | Observability | Custom lint rule checking logger calls | Lint warning → error | Month 1 |

### Layer 2: Architecture Tests

| Rule | Intent | Mechanism | Failure Mode | Priority |
|---|---|---|---|---|
| Dependency direction | Layering law | Import graph analysis: Domain→nothing, App→Domain, Adapters→App+Domain, Host→all | Test failure, CI blocks | Day 0 |
| No adapter imports in Application | Port abstraction | Architecture test: `src/application/` has no imports from `src/adapters/` | Test failure, CI blocks | Day 0 |
| No host imports in Adapters | Layer boundary | Architecture test: `src/adapters/` has no imports from `src/host/` | Test failure, CI blocks | Day 0 |
| No vendor type re-exports from adapters | Vendor containment | Architecture test: adapter public API check | Test failure, CI blocks | Week 1 |
| No business logic in host | Transport shell rule | Architecture test: complexity analysis of `src/host/routes/` | Test failure, CI blocks | Week 1 |
| Port interfaces in Application only | Ownership rule | Architecture test: port files exist only in `src/application/ports/` | Test failure, CI blocks | Week 1 |

### Layer 3: Scaffolding Enforcement

| Rule | Intent | Mechanism | Failure Mode | Priority |
|---|---|---|---|---|
| New capability uses scaffold | Structural consistency | CI check: new use case file must have matching test files | CI blocks | Week 1 |
| New port uses scaffold | Contract coverage | CI check: new port file must have matching contract test | CI blocks | Week 1 |
| New adapter uses scaffold | Certification | CI check: adapter test must import contract suite | CI blocks | Week 1 |
| Regression fix uses scaffold | Bug coverage | PR review: regression test file must reference bug ID | Review blocks | Month 1 |

### Layer 4: CI Gates

| Rule | Intent | Mechanism | Failure Mode | Priority |
|---|---|---|---|---|
| All test layers pass | Behavioral correctness | CI runs: domain, application, contract, adapter, regression, architecture | CI blocks | Day 0 (arch), Week 1 (rest) |
| Observability assertions pass | Telemetry correctness | CI runs observability test suite | CI blocks | Month 1 |
| No merge with failing checks | Enforcement integrity | Branch protection: all checks required | Merge blocked | Day 0 |
| Public contract change has updated tests | Contract drift prevention | CI check: changed port interface → changed contract test | CI blocks | Month 1 |
| Exception tracking | Accountability | CI check: constitution violation without exception record | CI warns/blocks | Month 1 |

### Layer 5: Runtime Guards

| Rule | Intent | Mechanism | Failure Mode | Priority |
|---|---|---|---|---|
| Config validated at startup | Fail fast | Startup validation function | App refuses to start | Week 1 |
| All ports bound at startup | Fail fast | DI container validation | App refuses to start | Week 1 |
| Correlation ID required | Traceability | Middleware guard | Request rejected | Week 1 |
| Operation name required | Traceability | Middleware guard | Request rejected | Week 1 |
| Auth context required (user requests) | Security | Auth middleware | Request rejected (401) | Week 1 |
| No duplicate capability registration | Consistency | DI container validation | App refuses to start | Month 1 |

## Phased Rollout

### Phase 1: Day 0 — Foundation (Must Exist Before First Capability)

- [ ] Directory structure created
- [ ] Architecture tests for dependency direction
- [ ] Lint rules for forbidden imports and APIs
- [ ] Standard Result type and AppError type
- [ ] Operation context type
- [ ] CI pipeline running lint + architecture tests
- [ ] Branch protection enabled

### Phase 2: Week 1 — First Capability with Full Compliance

- [ ] Scaffold generators for capability, port, adapter
- [ ] Startup validation (config + port bindings)
- [ ] Request-time guards (context, auth)
- [ ] Telemetry port with test adapter
- [ ] First capability passes all checks
- [ ] CI runs all test layers

### Phase 3: Month 1 — Full Enforcement

- [ ] CI gates for structural checks (new capability → tests exist)
- [ ] Observability assertion helpers
- [ ] Contract drift detection
- [ ] Exception registry with expiry tracking
- [ ] Multiple adapters certified
- [ ] All lint rules promoted from warning to error

### Phase 4: Later Hardening

- [ ] Mutation testing for domain layer
- [ ] Automated PR policy checks
- [ ] Advanced anti-pattern detection
- [ ] AI prompt templates
- [ ] Exception expiry automation

## Traceability Matrix

Every constitution section maps to enforcement:

| Constitution Section | Enforcement Layer(s) | Priority |
|---|---|---|
| §3 System Shape | Architecture tests | Day 0 |
| §4 Dependency Law | Architecture tests + lint | Day 0 |
| §5 Capability Rule | Scaffolding + CI | Week 1 |
| §6 Testing Constitution | CI gates | Day 0 – Month 1 |
| §7 Observability | Runtime guards + CI | Week 1 – Month 1 |
| §8 Determinism | Lint rules | Day 0 |
| §9 Error Constitution | Type system + lint + tests | Day 0 – Week 1 |
| §10 External Integration | Architecture tests + contracts | Week 1 |
| §11 Configuration | Runtime guards + lint | Week 1 |
| §12 Endpoint/Job Constitution | Architecture tests | Week 1 |
| §13 Mechanical Enforcement | All layers | Day 0 – Month 1 |
| §14 AI Usage | CI gates (same as human) | Day 0 |
| §15 Definition of Done | CI gates + review | Week 1 |
| §18 Exception Policy | CI + registry | Month 1 |
