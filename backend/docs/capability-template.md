# Capability Template

> Derived from [Backend Constitution](./backend-constitution.md) §5, §15.

## Purpose

Canonical template for defining a new capability. Every new capability must be created as a bounded slice containing all of the artifacts below. Copy this template and fill in every section.

---

## Capability Definition

### Name

`{CapabilityName}`

### Purpose

_{One-sentence description of what this capability does and why it exists.}_

### Layer

- [ ] Command (mutates state)
- [ ] Query (reads state)

---

## Input Contract

```typescript
interface {CapabilityName}Input {
  // Define all input fields with types
  // Example:
  // jobId: string
  // reason: string
}
```

**Validation rules:**

| Field | Rule |
|---|---|
| _{field}_ | _{validation rule}_ |

---

## Output Contract

```typescript
// Success
interface {CapabilityName}Output {
  // Define all output fields
}

// Result type
type {CapabilityName}Result = Result<{CapabilityName}Output, AppError>
```

---

## Invariants

| # | Invariant | Enforced By |
|---|---|---|
| 1 | _{invariant description}_ | _{domain entity / use case / port contract}_ |

---

## Failure Modes

| Error Code | Condition | Severity |
|---|---|---|
| `VALIDATION` | _{when input fails validation}_ | `warn` |
| `NOT_FOUND` | _{when resource does not exist}_ | `info` |
| `AUTHORIZATION` | _{when caller lacks permission}_ | `warn` |
| _{other codes from [Error Model](./error-model.md)}_ | | |

---

## Side Effects

| Side Effect | Port Used | Description |
|---|---|---|
| _{e.g., Persist state}_ | `{PortName}` | _{what changes}_ |
| _{e.g., Notify external system}_ | `{PortName}` | _{what is sent}_ |

---

## Observability Requirements

| Artifact | Details |
|---|---|
| Top-level span | `operationName: {CapabilityName}` |
| Structured log on completion | Includes `correlationId`, `result`, `latencyMs`, `errorCode` if failed |
| Success/failure metric | `capability.invocations { operationName: {CapabilityName}, result }` |
| Latency metric | `capability.latency { operationName: {CapabilityName} }` |
| Audit event | _{Required / Not required. If required, describe what is audited.}_ |
| External call spans | _{List each port call that must emit a child span}_ |

---

## Test Requirements

| Test Type | File | What It Proves |
|---|---|---|
| Domain spec | `tests/domain/{Entity}.spec.ts` | _{invariants, rules, transitions}_ |
| Application behavior | `tests/application/{CapabilityName}.spec.ts` | _{use case orchestration, error handling}_ |
| Port contract | `tests/contracts/{PortName}.contract.ts` | _{port behavioral contract}_ |
| Adapter | `tests/adapters/{AdapterName}.adapter.spec.ts` | _{adapter passes contract + vendor specifics}_ |
| Observability | `tests/application/{CapabilityName}.spec.ts` | _{telemetry emission}_ |

---

## Audit Requirements

- [ ] This capability is **not** a privileged action (no audit event needed)
- [ ] This capability **is** a privileged action — audit event required:

| Audit Field | Value |
|---|---|
| `action` | `{CapabilityName}` |
| `actor` | From operation context |
| `target` | _{what was acted upon}_ |
| `outcome` | `success` or `failure` |
| `detail` | _{relevant non-sensitive details}_ |

---

## Ports Required

| Port | Exists? | Needs Contract Tests? |
|---|---|---|
| `{PortName}` | Yes / **New** | Yes / Already covered |

---

## Scaffold Checklist

After running the capability scaffold generator, verify:

- [ ] Use case handler file created
- [ ] Input/output types defined
- [ ] Domain entity/service updated or created
- [ ] Port interface defined (if new)
- [ ] Domain spec test file created
- [ ] Application behavior test file created
- [ ] Port contract test file created (if new port)
- [ ] Observability assertions included in tests
- [ ] Registered in DI composition (host layer)
- [ ] Endpoint/job shell created (if externally triggered)

---

## Definition of Done

See [Definition of Done](./definition-of-done.md) for the full checklist. This capability is done when every item on that checklist is satisfied.
