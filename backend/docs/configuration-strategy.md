# Configuration Strategy

> Derived from [Backend Constitution](./backend-constitution.md) §11.

## Purpose

Define how configuration is typed, validated, accessed, and how secrets are handled. Configuration errors must be caught at startup, not at runtime.

## Rules

### 1. Typed Configuration

All configuration must be represented as typed objects, not arbitrary string lookups.

**Allowed:**
```typescript
interface AwsBatchConfig {
  region: string
  queueArn: string
  timeoutMs: number
  maxRetries: number
}

// Access via typed object
function createJobRunner(config: AwsBatchConfig) { ... }
```

**Forbidden:**
```typescript
// Magic string lookups
const region = config.get('AWS_BATCH_REGION')
const timeout = parseInt(process.env.TIMEOUT || '5000')
```

### 2. Startup Validation

All configuration must be validated at application startup. The application must fail fast on invalid values.

**Validated at startup:**
- Required fields are present
- Values are within acceptable ranges
- URLs are well-formed
- Enum values are recognized
- Secrets references resolve

**Failure mode:** Application refuses to start and logs a structured error identifying all invalid configuration fields.

### 3. Environment-Specific, Shape-Stable

Configuration shape is the same across all environments (dev, staging, production). Only values differ.

```typescript
// Shape is identical everywhere
interface AppConfig {
  server: ServerConfig
  database: DatabaseConfig
  aws: AwsBatchConfig
  jira: JiraConfig
  telemetry: TelemetryConfig
}
```

### 4. Secret Handling

| Rule | Detail |
|---|---|
| Secrets are never hardcoded | No secrets in source code |
| Secrets are never logged | Redact before logging |
| Secrets are not in typed config directly | Use a `SecretsPort` or reference/URI to secrets manager |
| Secrets are resolved at startup | Fail fast if secrets cannot be retrieved |
| Secrets are not passed through domain layer | Domain does not know about secrets |

**Allowed:**
```typescript
interface DatabaseConfig {
  host: string
  port: number
  database: string
  credentialsRef: string  // Reference to secrets manager
}
```

**Forbidden:**
```typescript
interface DatabaseConfig {
  host: string
  password: string  // Raw secret in typed config
}
```

### 5. No Config in Domain or Application Core

Domain and Application layers must not access configuration directly. Configuration is bound in the Host layer and injected through constructor parameters or port implementations.

| Layer | Config Access |
|---|---|
| Domain | **None.** Domain receives only domain types. |
| Application | **None.** Use cases receive ports (already configured). |
| Adapters | Receive typed config through constructor injection. |
| Host | Reads, validates, and binds config. Injects into adapters. |

### 6. Environment Overrides

Environment variables may override configuration values, but:
- Override mapping is explicit (not arbitrary `process.env` lookups)
- Overrides are validated the same way as base config
- Override resolution happens in one place (Host layer config binding)

## Configuration Validation Checklist

For every new configuration section:

- [ ] Typed interface defined
- [ ] Validation rules defined (required fields, ranges, formats)
- [ ] Startup validation implemented (fail-fast)
- [ ] Secrets use references, not raw values
- [ ] Environment override mapping is explicit
- [ ] Config is injected into adapters, not read by domain/application
- [ ] Sensitive values are redacted in logs
- [ ] Test exists for validation (valid and invalid inputs)

## Prohibited Patterns

| Pattern | Why |
|---|---|
| `process.env.X` in domain or application code | Config must not leak into core layers |
| `config.get('some.key')` untyped access | Loses type safety; errors appear at runtime |
| Default values hiding missing config | Fail-fast is required; silent defaults mask misconfiguration |
| Secrets as plain text in config files | Security violation |
| Config shape differs per environment | Divergence causes environment-specific bugs |

## Enforcement

| Rule | Mechanism |
|---|---|
| No `process.env` in domain/application | Static analyzer / lint rule |
| Typed config objects | Type system |
| Startup validation | Runtime guard (app refuses to start) |
| No secrets in config files | Secret scanning in CI |
| Config access only in host/adapters | Architecture test |

See [Runtime Guards](./runtime-guards.md), [CI Gates](./ci-gates.md).
