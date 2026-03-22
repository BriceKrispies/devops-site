# Port Contract Template

> Derived from [Backend Constitution](./backend-constitution.md) §6.2C, §10.

## Purpose

Canonical template for defining a port and its reusable contract test suite. Every port must have a contract suite that any adapter implementation can be certified against.

---

## Port Definition

### Name

`{PortName}`

### Purpose

_{One-sentence description of what this port abstracts.}_

### Interface

```typescript
interface {PortName} {
  // Define all methods with typed inputs and outputs
  // Every method returns Result<T, AppError>, never throws
  //
  // Example:
  // findById(id: string, ctx: OperationContext): Promise<Result<Entity, AppError>>
  // save(entity: Entity, ctx: OperationContext): Promise<Result<void, AppError>>
}
```

---

## Contract Test Suite

The contract test suite is a function that accepts an adapter factory and runs the full behavioral contract. Any adapter implementing this port must pass this suite.

### Structure

```typescript
// tests/contracts/{PortName}.contract.ts

export function {portName}ContractSuite(
  createAdapter: () => {PortName},
  setup?: () => Promise<void>,
  teardown?: () => Promise<void>
) {
  // All contract tests go here
}
```

### Normal Behavior

| Test | Description |
|---|---|
| `should save and retrieve entity` | Round-trip persistence works correctly |
| `should return not-found for non-existent entity` | Returns `NOT_FOUND` error, not exception |
| `should return correct data shape` | Output matches the port's output contract |
| _{add capability-specific behaviors}_ | |

### Edge Cases

| Test | Description |
|---|---|
| `should handle empty/minimal input` | Minimum valid input produces correct result |
| `should handle maximum-size input` | Large payloads within limits are handled |
| `should handle concurrent operations` | Concurrent calls do not corrupt state |
| _{add port-specific edge cases}_ | |

### Failure Behavior

| Test | Description |
|---|---|
| `should return typed error on validation failure` | Bad input → `VALIDATION` error |
| `should return typed error on not found` | Missing entity → `NOT_FOUND` error |
| `should return typed error on conflict` | Concurrent write → `CONFLICT` error |
| `should classify transient failures` | Retriable vendor error → `TRANSIENT_FAILURE` |
| `should classify permanent failures` | Non-retriable vendor error → `PERMANENT_FAILURE` |
| _{add port-specific failure cases}_ | |

### Timeout Behavior

| Test | Description |
|---|---|
| `should respect configured timeout` | Call does not hang indefinitely |
| `should return TIMEOUT error on deadline exceeded` | Timeout → `TIMEOUT` error |

### Retry Behavior

| Test | Description |
|---|---|
| `should retry on transient failure (if retries configured)` | Transient failure triggers retry |
| `should not retry on permanent failure` | Non-retriable errors are not retried |
| `should respect max retry count` | Retries stop after configured limit |

### Cancellation Behavior

| Test | Description |
|---|---|
| `should support cancellation signal` | Operation can be cancelled mid-flight |
| `should clean up on cancellation` | No partial state left after cancellation |

### Telemetry Expectations

| Test | Description |
|---|---|
| `should emit span for each call` | Span with `externalTarget`, `result`, `latencyMs` |
| `should emit metric for each call` | Counter for `external.calls` |
| `should emit error telemetry on failure` | Failed call includes `errorCode` in span and log |

### Invariants

| # | Invariant |
|---|---|
| 1 | Every method returns `Result<T, AppError>`, never throws |
| 2 | Errors are classified into [Error Model](./error-model.md) taxonomy |
| 3 | No vendor-specific types leak through the port interface |
| 4 | Operation context is propagated on every call |
| 5 | _{add port-specific invariants}_ |

---

## Adapter Certification Against This Contract

To certify a new adapter:

```typescript
// tests/adapters/{AdapterName}.adapter.spec.ts

import { {portName}ContractSuite } from '../contracts/{PortName}.contract'
import { {AdapterName} } from '../../src/adapters/{adapter-dir}/{AdapterName}'

describe('{AdapterName}', () => {
  {portName}ContractSuite(
    () => new {AdapterName}(/* config */),
    async () => { /* setup: seed data, start container, etc. */ },
    async () => { /* teardown: clean up */ }
  )

  // Additional adapter-specific tests below:
  // - Serialization boundaries
  // - Vendor-specific retry configuration
  // - Vendor-specific error mapping details
})
```

See [Adapter Certification](./adapter-certification.md) for the full certification checklist.

---

## Port Registration

Every port must be registered in the host layer's DI composition. The host must fail fast at startup if a required port has no bound adapter. See [Runtime Guards](./runtime-guards.md).
