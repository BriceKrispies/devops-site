# Exception Policy

> Derived from [Backend Constitution](./backend-constitution.md) §18.

## Purpose

Define how temporary exceptions to constitution rules are recorded, reviewed, expired, and removed. "Temporary" without an owner or expiry is not allowed.

## When an Exception Is Needed

An exception is required when:
- A PR needs to merge but violates a constitutional rule
- A known technical debt item exists and cannot be resolved immediately
- An external constraint prevents full compliance (e.g., vendor limitation)

## Exception Record Format

Every exception must be documented as follows:

| Field | Required | Description |
|---|---|---|
| `exception_id` | Yes | Unique identifier (e.g., `EXC-001`) |
| `rule_violated` | Yes | Which constitutional rule is being excepted |
| `justification` | Yes | Why the exception is necessary |
| `owner` | Yes | Person or team responsible for resolving |
| `created_date` | Yes | When the exception was created (ISO 8601) |
| `expiry_date` | Yes | When the exception must be resolved by (ISO 8601) |
| `scope` | Yes | Which files, capabilities, or adapters are affected |
| `mitigation` | Yes | What compensating controls exist while the exception is active |
| `tracking_issue` | Yes | Link to issue tracker item for resolution |
| `status` | Yes | `active`, `resolved`, `expired` |

## Exception Registry

Exceptions are tracked in a file at the repo root or in a dedicated tracking system:

**File-based:** `docs/exception-registry.md` — a table of all active exceptions.

| ID | Rule Violated | Owner | Expiry | Status |
|---|---|---|---|---|
| `EXC-001` | _{rule}_ | _{owner}_ | _{date}_ | `active` |

## Exception Lifecycle

```
Created → Active → Resolved / Expired
```

1. **Created:** Exception record is written with all required fields.
2. **Active:** Exception is in effect. Compensating controls are in place.
3. **Resolved:** The underlying issue is fixed. Exception record is marked `resolved`.
4. **Expired:** Expiry date passed without resolution. This triggers escalation.

## Review Cadence

- Active exceptions are reviewed at minimum every **2 weeks**.
- Expired exceptions are escalated to the team lead or engineering manager.
- Resolved exceptions remain in the registry for historical reference.

## Rules

| Rule | Detail |
|---|---|
| No exception without an owner | Someone must be accountable |
| No exception without an expiry date | Open-ended exceptions are not allowed |
| No exception without a tracking issue | Must be visible in issue tracking |
| No exception without justification | "We'll fix it later" is not justification |
| No exception without mitigation | What prevents the violation from causing harm? |
| Expired exceptions trigger escalation | Ignoring expiry is not allowed |
| Exceptions are visible in CI | CI should flag or annotate exception-covered code |

## Prohibited Patterns

| Pattern | Why |
|---|---|
| Undocumented `// TODO: fix later` | Not an exception; invisible to enforcement |
| Verbal agreement to skip a rule | Not documented; not tracked; not enforceable |
| Exception with no expiry | Becomes permanent tech debt |
| Exception owned by "the team" | No individual accountability |

## Enforcement

| Rule | Mechanism |
|---|---|
| Exceptions are documented | PR review: constitution violation requires exception record |
| Exceptions have expiry | CI or scheduled check: flag expired exceptions |
| Exceptions are tracked | Issue tracker integration |
| Expired exceptions are escalated | Automated notification or CI warning |

See [CI Gates](./ci-gates.md).
