# Runtime Guards

> Derived from [Backend Constitution](./backend-constitution.md) §13.5.

## Purpose

Define fail-fast conditions and runtime invariant checks. The application must detect invalid state as early as possible and refuse to proceed, rather than silently corrupting data or producing incorrect results.

## Fail-Fast Conditions

### Startup Guards

These checks run during application startup. If any fail, the application must refuse to start and log a structured error.

| Guard | Condition | Failure Behavior |
|---|---|---|
| Invalid config | Any configuration field fails validation | Log all invalid fields, exit with non-zero |
| Missing secrets | Secret references cannot be resolved | Log which secrets are missing, exit |
| Missing adapter bindings | A required port has no adapter registered in DI | Log unbound ports, exit |
| Duplicate capability registration | Two handlers registered for the same capability name | Log the duplicate, exit |
| Invalid telemetry config | Telemetry exporter cannot be initialized | Log the failure, exit |
| Database connectivity (if applicable) | Required database is unreachable at startup | Log connection failure, exit |

### Request-Time Guards

These checks run on every incoming request or operation. If any fail, the operation is rejected immediately.

| Guard | Condition | Failure Behavior |
|---|---|---|
| Missing correlation ID | Operation context has no `correlationId` | Reject with `INVARIANT_VIOLATION`, log `fatal` |
| Missing operation name | Operation context has no `operationName` | Reject with `INVARIANT_VIOLATION`, log `fatal` |
| Missing actor (user request) | User-initiated request has no authenticated actor | Reject with `AUTHORIZATION` error |
| Missing tenant ID (multi-tenant) | Tenant-scoped operation has no `tenantId` | Reject with `VALIDATION` error |
| Invalid auth context | Auth token is expired, malformed, or revoked | Reject with `AUTHORIZATION` error |

### Domain Invariant Guards

These checks run inside domain entities and value objects. If any fail, the operation is aborted.

| Guard | Condition | Failure Behavior |
|---|---|---|
| Entity invariant violation | Business rule precondition fails | Return `INVARIANT_VIOLATION` error |
| Value object invalid state | Value object constructed with invalid data | Return `VALIDATION` error or throw at construction |
| State transition violation | Attempted transition is not allowed from current state | Return `CONFLICT` error |

## Where Guards Live

| Guard Type | Layer | Implementation |
|---|---|---|
| Startup guards | Host | Composition root / startup sequence |
| Request-time guards | Host | Middleware / request pipeline |
| Correlation ID / context guards | Host | Middleware or context factory |
| Auth/tenant guards | Host | Auth middleware |
| Domain invariant guards | Domain | Entity constructors, methods, value object factories |
| Port binding guards | Host | DI container validation |

## Guard Implementation Pattern

```typescript
// Startup guard example (pseudocode)
function validateStartup(container: Container, config: AppConfig): void {
  const errors: string[] = []

  // Config validation
  const configErrors = validateConfig(config)
  errors.push(...configErrors)

  // Port binding validation
  for (const portName of requiredPorts) {
    if (!container.isBound(portName)) {
      errors.push(`Missing adapter binding for port: ${portName}`)
    }
  }

  if (errors.length > 0) {
    logger.fatal({ errors }, 'Startup validation failed')
    process.exit(1)
  }
}
```

```typescript
// Request-time guard example (pseudocode)
function contextGuard(ctx: OperationContext): Result<void, AppError> {
  if (!ctx.correlationId) {
    return fail({
      code: 'INVARIANT_VIOLATION',
      message: 'Missing correlationId in operation context',
      severity: 'fatal',
    })
  }
  if (!ctx.operationName) {
    return fail({
      code: 'INVARIANT_VIOLATION',
      message: 'Missing operationName in operation context',
      severity: 'fatal',
    })
  }
  return ok()
}
```

## Prohibited Patterns

| Pattern | Why |
|---|---|
| Silently using default values for missing config | Masks misconfiguration; fails later in unexpected ways |
| Continuing without required port bindings | Null reference at runtime instead of clear startup failure |
| Ignoring missing correlation ID | Breaks tracing and observability chain |
| Catching invariant violations and continuing | Invariant violations mean the system is in an unexpected state |
| Logging a warning instead of failing | Warnings are ignored; fail-fast is not optional |

## Enforcement

| Rule | Mechanism |
|---|---|
| Startup guards run | Integration test: app startup with invalid config fails |
| Context guards run | Application behavior test: request without context is rejected |
| Domain invariants enforced | Domain spec tests |
| All required ports bound | Startup guard + test with missing binding |

See [CI Gates](./ci-gates.md), [Testing Strategy](./testing-strategy.md).
