# Scaffolding Strategy

> Derived from [Backend Constitution](./backend-constitution.md) §13.3.

## Purpose

Define what approved scaffolds must create for each type of new artifact. Scaffolding ensures every new slice starts with the correct structure, required tests, and observability hooks — reducing AI drift and human error.

## Principle

New capabilities, ports, adapters, endpoints, and regression fixes must be created through approved scaffolds. Bypassing scaffolds is a constitution violation.

## Scaffold Definitions

### 1. New Capability

**Command:** `scaffold capability {CapabilityName}`

**Output artifacts:**

| File | Layer | Content |
|---|---|---|
| `src/application/use-cases/{CapabilityName}.ts` | Application | Use case handler skeleton with `OperationContext`, `Result` return type |
| `src/application/use-cases/{CapabilityName}.input.ts` | Application | Typed input contract |
| `src/application/use-cases/{CapabilityName}.output.ts` | Application | Typed output contract |
| `tests/application/{CapabilityName}.spec.ts` | Tests | Behavior test skeleton with success, failure, and observability assertions |
| `tests/domain/{Entity}.spec.ts` | Tests | Domain spec skeleton (if new entity) |

**Skeleton includes:**
- Import of `OperationContext`
- Import of `Result` and `AppError`
- Empty test cases for: success path, validation failure, not-found, observability emission
- Telemetry span start/end
- Structured log on completion

### 2. New Port

**Command:** `scaffold port {PortName}`

**Output artifacts:**

| File | Layer | Content |
|---|---|---|
| `src/application/ports/{PortName}.ts` | Application | Port interface definition |
| `tests/contracts/{PortName}.contract.ts` | Tests | Reusable contract test suite skeleton |

**Skeleton includes:**
- Interface with methods returning `Result<T, AppError>`
- Contract suite function accepting adapter factory
- Empty test cases for: normal behavior, edge cases, failure behavior, timeout, telemetry

### 3. New Adapter

**Command:** `scaffold adapter {AdapterName} --port {PortName}`

**Output artifacts:**

| File | Layer | Content |
|---|---|---|
| `src/adapters/{adapter-dir}/{AdapterName}.ts` | Adapters | Adapter implementation skeleton |
| `tests/adapters/{AdapterName}.adapter.spec.ts` | Tests | Adapter test importing and running contract suite |

**Skeleton includes:**
- Class implementing the port interface
- Constructor accepting typed config
- Error mapping placeholder (vendor errors → internal taxonomy)
- Telemetry span emission in each method
- Test file that imports the port contract suite and runs it

### 4. New Endpoint/Job/Consumer Shell

**Command:** `scaffold endpoint {EndpointName}` / `scaffold job {JobName}` / `scaffold consumer {ConsumerName}`

**Output artifacts:**

| File | Layer | Content |
|---|---|---|
| `src/host/routes/{EndpointName}.ts` (or `jobs/` or `consumers/`) | Host | Transport shell skeleton |

**Skeleton includes:**
- Input parsing
- `OperationContext` creation
- Use case invocation
- Response mapping
- No business logic placeholder
- Comment: "Business logic belongs in Application layer"

### 5. Regression Fix

**Command:** `scaffold regression {BugId}`

**Output artifacts:**

| File | Layer | Content |
|---|---|---|
| `tests/regression/{BugId}.regression.spec.ts` | Tests | Regression test skeleton with bug ID reference |

**Skeleton includes:**
- Comment referencing the bug ID or incident
- Empty test case: "should reproduce the original failure"
- Empty test case: "should pass after the fix"

## Scaffold Output Validation

After running a scaffold, the following must be true:

- [ ] All output files exist
- [ ] Test files contain at least one `describe` and one `it`/`test` block
- [ ] Use case file returns `Result<T, AppError>`
- [ ] Port interface methods return `Result<T, AppError>`
- [ ] Adapter test imports the contract suite

## How Scaffolding Reduces AI Drift

| Risk | How Scaffolding Mitigates |
|---|---|
| AI places business logic in host layer | Scaffold creates use case file; host shell has no logic placeholder |
| AI skips test creation | Scaffold creates test files with required structure |
| AI forgets observability | Scaffold includes telemetry hooks and observability test cases |
| AI invents non-standard error types | Scaffold imports standard `AppError` and `ErrorCode` |
| AI bypasses port abstraction | Scaffold creates port interface; adapter implements it |
| AI uses untyped config | Scaffold adapter constructor accepts typed config |

## Enforcement

| Rule | Mechanism |
|---|---|
| New capability uses scaffold | CI check: new use case file must have corresponding test files |
| New port uses scaffold | CI check: new port file must have contract test |
| New adapter uses scaffold | CI check: adapter test imports contract suite |
| Scaffolds are not bypassed | PR review checklist + CI structural checks |

See [CI Gates](./ci-gates.md), [AI Contribution Rules](./ai-contribution-rules.md).
