# Audit & Admin Actions

> Derived from [Backend Constitution](./backend-constitution.md) §7.5, §14.4.

## Purpose

Define what actions require audit records, the minimum audit event schema, and retention expectations.

## Connection to Authorization Model (Constitution §14)

Privileged capabilities are identified by `CapabilityDescriptor.IsPrivileged = true`. When a capability is privileged:

- `RequiresAudit` must be `true` (enforced by `CapabilityDescriptor.Validate()`)
- `RequiresAuthentication` must be `true`
- The handler must emit audit events via `IAuditPort`
- Architecture tests verify this invariant (`SecurityMetadataTests.Privileged_descriptors_must_require_audit`)

## Actions Requiring Audit Records

Every privileged or operationally meaningful action must emit an audit event. This includes:

| Category | Examples |
|---|---|
| Job management | Rerunning jobs, cancelling jobs, force-completing jobs |
| Flag/config changes | Changing feature flags, updating operational config |
| Access management | Granting/revoking roles, modifying permissions |
| Workflow operations | Replaying workflows, resetting pipeline stages |
| Administrative overrides | Bypassing approval gates, overriding automated decisions |
| Manual remediation | Manually fixing data, clearing queues, resetting state |
| Deployment actions | Triggering deploys, rolling back deployments |
| Secret management | Rotating secrets, updating credentials |

## Audit Event Schema

Every audit event must include at minimum:

| Field | Type | Required | Description |
|---|---|---|---|
| `eventId` | string | Yes | Unique identifier for this audit event |
| `timestamp` | string (ISO 8601) | Yes | When the action occurred |
| `action` | string | Yes | Name of the action (matches capability name) |
| `actor` | ActorIdentity | Yes | Who performed the action |
| `target` | string | Yes | What was acted upon (resource type + ID) |
| `outcome` | `success` \| `failure` | Yes | Result of the action |
| `correlationId` | string | Yes | Links to the operation context |
| `tenantId` | string | If applicable | Scope/tenant |
| `detail` | object | Optional | Relevant non-sensitive details of the action |
| `reason` | string | Optional | Why the action was taken (for manual/override actions) |

**Example:**
```json
{
  "eventId": "audit-789",
  "timestamp": "2026-03-22T14:30:00Z",
  "action": "RerunJob",
  "actor": { "id": "user-123", "type": "user", "displayName": "Jane Smith" },
  "target": "Job/job-456",
  "outcome": "success",
  "correlationId": "corr-abc-123",
  "detail": { "previousStatus": "failed", "newStatus": "pending" },
  "reason": "Transient infrastructure failure resolved"
}
```

## What Must NOT Be in Audit Events

| Prohibited | Reason |
|---|---|
| Secrets, tokens, passwords | Security violation |
| PII beyond actor identity | Privacy compliance |
| Full request/response payloads | Size, security; link to correlation ID instead |
| Internal stack traces | Implementation detail; not useful for audit |

## Audit Event Durability

Audit events are not ordinary logs. They must be:

- **Durable:** Stored in a persistent, queryable store (not just log files)
- **Immutable:** Cannot be modified or deleted by application code
- **Queryable:** Can be searched by actor, action, target, time range
- **Retained:** Retention period defined per compliance requirements (minimum: discuss with compliance)

## Audit in Tests

For capabilities that require audit events:

- [ ] Test asserts an audit event was emitted on success
- [ ] Test asserts an audit event was emitted on failure (with `outcome: failure`)
- [ ] Test asserts required fields are present
- [ ] Test asserts sensitive data is not included

Use a fake/in-memory audit store in tests.

## Implementation Location

| Concern | Layer |
|---|---|
| Audit event type definition | Application |
| Audit port interface | Application (`AuditPort`) |
| Audit store adapter | Adapters |
| Audit emission in use case | Application (use case handler emits via `AuditPort`) |
| Audit store wiring | Host |

## Enforcement

| Rule | Mechanism |
|---|---|
| Privileged actions emit audit events | Observability assertion tests |
| Audit events include required fields | Type system + test assertions |
| No sensitive data in audit events | Code review + test assertions |
| Audit store is registered at startup | Runtime guard |

See [Observability Strategy](./observability-strategy.md), [Runtime Guards](./runtime-guards.md).
