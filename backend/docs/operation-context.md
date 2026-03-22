# Operation Context

> Derived from [Backend Constitution](./backend-constitution.md) §7.1, §8.

## Purpose

Define the standard operation context model that every command, query, and background operation must carry. This context enables tracing, logging, auditing, and enforcement of tenant/actor scoping.

## Operation Context Model

```typescript
interface OperationContext {
  // Required
  correlationId: string     // Unique ID for the entire request/workflow chain
  operationName: string     // Name of the capability being executed

  // Required where applicable
  causationId?: string      // ID of the event/operation that triggered this one
  actor?: ActorIdentity     // Who initiated this operation
  tenantId?: string         // Scope/tenant identifier

  // Metadata
  timestamp: string         // ISO 8601 timestamp of operation start
  source: OperationSource   // How the operation was triggered

  // Authorization (Constitution §14)
  permissions: Set<Permission>  // Normalized internal permissions resolved from actor identity
  isAuthenticated: boolean      // Derived: true when actor is present
}

interface ActorIdentity {
  id: string                // Unique identifier (user ID, service account ID)
  type: 'user' | 'service' | 'system' | 'scheduler'
  displayName?: string      // For audit log readability
}

type OperationSource = 'http-request' | 'background-job' | 'queue-message' | 'scheduler' | 'internal-workflow'
```

## Required vs Optional Fields by Source

| Field | HTTP Request | Background Job | Queue Message | Scheduler | Internal Workflow |
|---|---|---|---|---|---|
| `correlationId` | **Required** | **Required** | **Required** | **Required** | **Required** |
| `operationName` | **Required** | **Required** | **Required** | **Required** | **Required** |
| `causationId` | Optional | From trigger | From message | Optional | **Required** |
| `actor` | **Required** | From trigger or `system` | From message | `scheduler` | From parent |
| `tenantId` | **Required** (if multi-tenant) | From trigger | From message | Scope-specific | From parent |
| `timestamp` | **Required** | **Required** | **Required** | **Required** | **Required** |
| `source` | `http-request` | `background-job` | `queue-message` | `scheduler` | `internal-workflow` |
| `permissions` | From identity claims | From trigger | From message | Service perms | From parent |

## Creation Rules

| Trigger | Who Creates Context | Notes |
|---|---|---|
| HTTP request | Host middleware | Extract actor from auth token, generate correlationId |
| Background job | Job runner shell | Inherit correlationId from trigger if available, otherwise generate new |
| Queue message | Consumer shell | Extract context from message headers/metadata |
| Scheduler | Scheduler shell | Generate correlationId, actor = scheduler identity |
| Internal workflow step | Calling use case | Carry parent correlationId, set causationId = parent operationId |

## Propagation Rules

1. **correlationId** propagates through the entire call chain, including across async boundaries and external calls.
2. **causationId** is set to the ID of the direct parent operation when one operation triggers another.
3. When calling an external system through a port, the adapter must include `correlationId` in outbound headers/metadata where the external system supports it.
4. When receiving a message from a queue, the consumer must extract the `correlationId` from message metadata.

## Validation Rules

| Rule | Enforcement |
|---|---|
| `correlationId` must be a non-empty string | Runtime guard at context creation |
| `operationName` must be a non-empty string | Runtime guard at context creation |
| `actor` must be present for user-initiated operations | Runtime guard in middleware |
| `tenantId` must be present for tenant-scoped operations | Runtime guard in middleware |
| `timestamp` must be valid ISO 8601 | Type system + validation at creation |

## What Happens When Context Is Missing

| Missing Field | Behavior |
|---|---|
| `correlationId` | **Fail fast.** Reject the operation. Log a `fatal` entry. |
| `operationName` | **Fail fast.** Reject the operation. |
| `actor` (user request) | **Fail fast.** Return authorization error. |
| `tenantId` (multi-tenant) | **Fail fast.** Return validation error. |
| `causationId` | Proceed — this field is optional in many contexts |

See [Runtime Guards](./runtime-guards.md).

## Context in Logs, Traces, and Metrics

Every structured log entry, trace span, and metric emission must include at minimum:
- `correlationId`
- `operationName`

Every audit event must additionally include:
- `actor`
- `tenantId` (if applicable)

See [Observability Strategy](./observability-strategy.md).

## Forbidden Patterns

| Pattern | Why |
|---|---|
| Creating context deep inside application logic | Context must be created at the boundary (host layer) |
| Passing context fields as individual parameters | Use the OperationContext object |
| Generating correlationId inside domain logic | Domain must not have side effects; IDs come from the boundary |
| Ignoring context in background jobs | Background jobs are operations too; they need full context |
| Storing mutable state on context | Context is immutable once created |
