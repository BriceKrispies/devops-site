# Layering Rules

> Derived from [Backend Constitution](./backend-constitution.md) §3–4. Enforced by [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md).

## Purpose

Provide explicit, mechanically enforceable rules for what each layer may and may not import, reference, or depend on.

## Layer Dependency Matrix

| Source Layer | Domain | Application | Adapters | Host | External/Vendor |
|---|---|---|---|---|---|
| **Domain** | ✅ internal | ❌ | ❌ | ❌ | ❌ |
| **Application** | ✅ | ✅ internal | ❌ | ❌ | ❌ |
| **Adapters** | ✅ | ✅ | ✅ internal | ❌ | ✅ |
| **Host** | ✅ | ✅ | ✅ | ✅ internal | ✅ (wiring only) |

## Rules by Layer

### Domain Layer

**Allowed imports:**
- Other Domain modules
- Language standard library (pure utilities: string, math, collections)

**Forbidden imports:**
- `src/application/*`
- `src/adapters/*`
- `src/host/*`
- Any database driver or ORM package
- Any HTTP client or server package
- Any cloud SDK (`aws-sdk`, `@aws-sdk/*`, `@octokit/*`, `jira-client`, etc.)
- Any framework package (Express, Fastify, NestJS decorators, etc.)
- Any logging library directly (Winston, Pino, etc.)
- `Date.now()`, `new Date()` without clock abstraction
- `Math.random()` without random provider abstraction
- `process.env` or any direct environment access
- `fs`, `path`, `child_process`, `net`

**Architecture test assertions:**
```
- No file in src/domain/ imports from src/application/
- No file in src/domain/ imports from src/adapters/
- No file in src/domain/ imports from src/host/
- No file in src/domain/ imports any package matching vendor SDK patterns
- No file in src/domain/ calls Date.now() or new Date()
- No file in src/domain/ calls Math.random()
- No file in src/domain/ reads process.env
```

### Application Layer

**Allowed imports:**
- Domain modules
- Other Application modules
- Port interface definitions (defined within Application)

**Forbidden imports:**
- `src/adapters/*`
- `src/host/*`
- Any vendor SDK
- Any database driver or ORM
- Any HTTP client library
- Framework decorators (e.g., `@Controller`, `@Injectable` if they leak framework types)

**Architecture test assertions:**
```
- No file in src/application/ imports from src/adapters/
- No file in src/application/ imports from src/host/
- No file in src/application/ imports any vendor SDK package
- No file in src/application/ contains SQL strings
- No file in src/application/ imports HTTP client libraries
```

### Adapters Layer

**Allowed imports:**
- Application modules (port interfaces, result types, error types)
- Domain modules (entities, value objects)
- Other Adapter modules (shared adapter utilities)
- External vendor SDKs and libraries
- Telemetry libraries

**Forbidden imports:**
- `src/host/*`
- Must not re-export vendor types to Application or Domain

**Architecture test assertions:**
```
- No file in src/adapters/ imports from src/host/
- No adapter module exports vendor-specific types in its public API
```

### Host Layer

**Allowed imports:**
- All internal layers (for wiring)
- Framework packages
- Configuration libraries

**Forbidden patterns:**
- Business logic in route handlers, middleware, or startup code
- Direct SQL or vendor SDK calls for business purposes (use injected ports)

**Architecture test assertions:**
```
- Route handler functions contain only: parse, invoke use case, map response
- No business conditionals in host layer files
```

## Directory Mapping

Expected project structure:

```
src/
├── domain/           # Domain layer
│   ├── entities/
│   ├── value-objects/
│   ├── services/
│   └── invariants/
├── application/      # Application layer
│   ├── use-cases/
│   ├── ports/        # Port interface definitions
│   ├── errors/       # Error taxonomy
│   └── results/      # Result types
├── adapters/         # Adapters layer
│   ├── jira/
│   ├── aws/
│   ├── grafana/
│   ├── okta/
│   ├── github/
│   ├── persistence/
│   └── telemetry/
├── host/             # Host layer
│   ├── routes/
│   ├── middleware/
│   ├── config/
│   └── composition/
└── tests/
    ├── domain/
    ├── application/
    ├── adapters/
    ├── contracts/    # Port contract test suites
    └── architecture/ # Architecture enforcement tests
```

## Enforcement

| Mechanism | What It Checks |
|---|---|
| Architecture tests | Import graph matches allowed dependency matrix |
| Static analyzer rules | Forbidden API calls in Domain (Date.now, Math.random, process.env) |
| Lint rules | No vendor SDK imports in Domain or Application |
| CI gate | All of the above must pass before merge |

See [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md) for implementation details.
