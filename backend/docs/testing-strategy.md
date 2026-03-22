# Testing Strategy

> Derived from [Backend Constitution](./backend-constitution.md) §6.

## Purpose

Define the test taxonomy, what each test type proves, naming conventions, directory layout, and banned anti-patterns. Tests are first-class system artifacts — they are the executable definition of behavior.

## Test Taxonomy

### 1. Domain Specification Tests

**What they prove:** Business rules, invariants, state transitions, edge cases, and regression cases work correctly.

**What they must not do:**
- Mock internal domain behavior
- Depend on infrastructure (databases, HTTP, file system)
- Test implementation details (private methods, internal data structures)

**Location:** `src/tests/domain/`

**Naming:** `{Entity|Service|ValueObject}.spec.{ts|js}`

**Example scope:**
- Entity creation enforces invariants
- State transition from A to B is only allowed when precondition holds
- Value object equality and immutability
- Domain service computes correct result for edge cases

### 2. Application Behavior Tests

**What they prove:** Use cases orchestrate domain logic and ports correctly, handle errors, enforce policies, and produce correct results.

**What they must not do:**
- Assert implementation trivia (e.g., "port method was called exactly 3 times")
- Contain business rules (those belong in Domain)
- Use real adapters

**Allowed:** Fake/stub port implementations that satisfy the port contract.

**Location:** `src/tests/application/`

**Naming:** `{UseCaseName}.spec.{ts|js}`

**Example scope:**
- Use case returns success result when domain rules pass
- Use case returns typed error when validation fails
- Use case calls persistence port after domain operation
- Use case handles port failure and classifies error correctly

### 3. Port Contract Tests

**What they prove:** Any adapter implementing a given port satisfies the behavioral contract.

**What they must not do:**
- Be tied to a specific adapter implementation
- Assert vendor-specific behavior

**Structure:** Each port has a reusable contract test suite exported as a function that accepts an adapter factory.

**Location:** `src/tests/contracts/`

**Naming:** `{PortName}.contract.{ts|js}`

**Example scope:**
- Save then retrieve returns equivalent entity
- Retrieve non-existent returns not-found error
- Concurrent writes are handled according to contract
- Timeout behavior complies with contract

See [Port Contract Template](./port-contract-template.md).

### 4. Adapter Tests

**What they prove:** A specific adapter implementation passes the port contract suite and handles vendor-specific concerns correctly.

**What they must not do:**
- Duplicate the contract test suite (run it, don't rewrite it)
- Skip failure, timeout, or retry scenarios

**Location:** `src/tests/adapters/`

**Naming:** `{AdapterName}.adapter.spec.{ts|js}`

**Required coverage:**
- Passes full port contract suite
- Serialization/deserialization boundaries
- Retry behavior
- Timeout behavior
- Failure classification (vendor errors → internal error taxonomy)
- Telemetry emission

See [Adapter Certification](./adapter-certification.md).

### 5. Regression Tests

**What they prove:** A previously observed production bug cannot recur.

**Rule:** Every production bug must begin with a failing test before the fix is accepted.

**Location:** `src/tests/regression/`

**Naming:** `{BugId|Description}.regression.spec.{ts|js}`

**Required:**
- Test reproduces the original failure
- Test passes after the fix
- Test references the bug ID or incident

### 6. Observability Assertion Tests

**What they prove:** Critical capabilities emit required telemetry (logs, metrics, spans, audit events).

**Location:** Colocated with the capability's application behavior tests.

**Naming:** Included as a section within `{UseCaseName}.spec.{ts|js}` or as `{UseCaseName}.observability.spec.{ts|js}`.

**Example scope:**
- Use case emits span with correct operation name
- Use case emits structured log with correlation ID
- Use case increments success/failure metric counter
- Privileged action emits audit event

### 7. Architecture Tests

**What they prove:** Dependency directions, namespace boundaries, and layering rules are not violated.

**Location:** `src/tests/architecture/`

**Naming:** `{RuleName}.arch.spec.{ts|js}`

See [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md).

## Test Naming Conventions

| Convention | Format |
|---|---|
| Test file | `{Subject}.{type}.{ts|js}` where type is `spec`, `contract`, `adapter.spec`, `regression.spec`, `arch.spec`, `observability.spec` |
| Test suite (describe) | Name of the unit under test |
| Test case (it/test) | `should {expected behavior} when {condition}` |

## Directory Layout

```
src/tests/
├── domain/              # Domain specification tests
├── application/         # Application behavior tests
├── contracts/           # Port contract test suites (reusable)
├── adapters/            # Adapter-specific tests (run contract suites + extras)
├── regression/          # Bug regression tests
├── architecture/        # Architecture enforcement tests
└── helpers/             # Shared test utilities, fakes, builders
```

## Banned Test Patterns

| Anti-Pattern | Why It Is Banned |
|---|---|
| Testing private methods | Couples tests to implementation; test through public API instead |
| Assert call count as primary correctness proof | Proves coupling, not behavior |
| Mock-heavy tests with no behavioral meaning | Tests pass but prove nothing about correctness |
| Asserting implementation details instead of contract | Fragile; breaks on refactor without behavior change |
| Giant end-to-end tests substituting for missing specs | Slow, flaky, hard to diagnose; write targeted specs instead |
| Tests that pass only because of test execution order | All tests must be independently runnable |
| Tests that require real external services without adapter boundary | Use contract test suites against controlled environments |
| Snapshot tests for business logic | Approve-and-forget; does not prove invariants |

## Coverage Rules

Coverage is a lagging indicator, not the target. The target is:

| Coverage Type | Meaning |
|---|---|
| Behavioral coverage | Every specified behavior has a test |
| Invariant coverage | Every domain invariant is asserted |
| Contract coverage | Every port contract is tested |
| Failure mode coverage | Every classified error path has a test |

Line coverage may be tracked but does not make code safe by itself.

## Mutation Testing

Critical core logic (Domain layer) should adopt mutation testing to expose weak tests. A test suite that achieves high line coverage but has low mutation kill rate is insufficient.

## Enforcement

| Rule | Mechanism |
|---|---|
| New capability must have spec tests | CI gate: scaffold generates test files; CI fails if test files are empty or missing |
| New adapter must pass contract suite | CI gate: adapter test must import and execute the contract suite |
| Regression fix must include failing test | PR review checklist + CI check for test file in regression directory |
| Architecture tests must pass | CI gate: architecture test suite runs on every PR |
| No banned test patterns | Code review + lint rules where automatable |

See [CI Gates](./ci-gates.md).
