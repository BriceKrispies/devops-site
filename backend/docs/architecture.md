# Architecture

> Derived from [Backend Constitution](./backend-constitution.md) §3–4, §12.

## Purpose

Define the system shape, layer boundaries, dependency directions, and capability flow so that every contributor and every automated check shares the same structural model.

## System Layers

The backend has exactly four layers. No other layers exist.

| Layer | Responsibility | May Depend On |
|---|---|---|
| **Domain** | Entities, value objects, domain services, invariants, pure business rules | Nothing outside Domain |
| **Application** | Use cases, commands/queries, orchestration, port definitions, result types, error taxonomy | Domain |
| **Adapters** | AWS/Jira/Grafana/Okta/GitHub integrations, repositories, caches, queues, HTTP clients, telemetry plumbing | Application, Domain |
| **Host** | DI composition, startup, middleware, config binding, endpoint registration, runtime wiring | Adapters, Application, Domain |

## Dependency Law

```
Host ──▶ Adapters ──▶ Application ──▶ Domain
  │                        │
  └──▶ Application         └──▶ Domain
  └──▶ Domain
```

### Allowed

| From | To |
|---|---|
| Host | Adapters, Application, Domain |
| Adapters | Application, Domain |
| Application | Domain |
| Domain | Domain (internal only) |

### Forbidden

| From | To | Reason |
|---|---|---|
| Domain | Application, Adapters, Host | Domain must be pure and self-contained |
| Domain | Any framework or vendor package | Domain must be replaceable |
| Application | Adapters | Application defines ports; it never references concrete adapters |
| Application | Host | Application must not know about startup or wiring |
| Application | Any vendor SDK | SDKs are adapter-only |

See [Layering Rules](./layering-rules.md) for concrete import rules and analyzer assertions.

## Layer Contents

### Domain

**Contains:**
- Domain entities with enforced invariants
- Value objects (immutable, equality by value)
- Domain services (stateless, pure logic)
- Domain events (if applicable)
- Business rules expressed as functions or methods

**Must not contain:**
- Database code, SQL, ORM annotations
- HTTP code, request/response types
- Cloud SDK calls (AWS, Azure, GCP)
- File system access
- Direct time access (`Date.now()`, `new Date()`) — use a `Clock` port
- Direct random access — use a `RandomProvider` port
- Logging implementation details — use a `Logger` port if needed
- Framework decorators or annotations

### Application

**Contains:**
- Use case handlers (command handlers, query handlers)
- Port interfaces (abstractions for external systems)
- Result types (`Result<T, E>` or equivalent)
- Error taxonomy types
- Transaction boundary abstractions
- Application-level validation rules

**Must not contain:**
- Vendor SDKs
- SQL or query builder code
- HTTP client calls
- Framework-specific controller/handler decorators
- Business rules hidden in orchestration (push these to Domain)

### Adapters

**Contains:**
- Concrete implementations of ports defined in Application
- AWS SDK calls, Jira API clients, Grafana query clients
- Database repository implementations
- Cache implementations (Redis, Memcached)
- Queue implementations (SQS, RabbitMQ)
- HTTP transport adapters
- Telemetry plumbing (OpenTelemetry exporters, log sinks)

**Rules:**
- May depend on external libraries and vendor SDKs
- Must map vendor-specific errors into the internal [error taxonomy](./error-model.md)
- Must implement standardized retry, timeout, and cancellation behavior
- Must emit telemetry as specified in [Observability Strategy](./observability-strategy.md)
- Must pass the [port contract test suite](./port-contract-template.md) for every port they implement

### Host

**Contains:**
- Dependency injection container composition
- Application startup and shutdown
- Middleware registration
- Configuration binding and validation
- Endpoint/route registration
- Runtime wiring (connecting adapters to ports)

**Rules:**
- No business rules
- No direct vendor SDK usage for business purposes
- Transport endpoints are thin shells: parse input → invoke use case → map output
- See [Runtime Guards](./runtime-guards.md) for startup validation

## Capability Flow

A typical request flows through the system as follows:

```
[HTTP Request / Job Trigger / Queue Message]
        │
        ▼
   Host (endpoint/job shell)
   - Parse input
   - Build OperationContext
   - Invoke use case
        │
        ▼
   Application (use case handler)
   - Validate command/query
   - Orchestrate domain logic via ports
   - Return Result<T, E>
        │
        ▼
   Domain (business rules)
   - Enforce invariants
   - Apply state transitions
   - Return domain result
        │
        ▼
   Application (use case handler)
   - Call ports for side effects (persistence, notifications, external systems)
        │
        ▼
   Adapters (port implementations)
   - Translate to vendor-specific calls
   - Map vendor errors to internal taxonomy
   - Emit telemetry
        │
        ▼
   Host (endpoint/job shell)
   - Map Result<T, E> to transport response
   - Return HTTP response / mark job complete
```

## Why Ports Exist

Ports exist so that:

1. **Domain and Application logic is testable** without real infrastructure.
2. **Vendor implementations are replaceable** — swapping Jira for Linear, or PostgreSQL for DynamoDB, requires only a new adapter that passes the same contract tests.
3. **External system failures are classified uniformly** — every adapter maps vendor errors into the same taxonomy.
4. **AI agents can generate adapters** from port definitions without touching business logic.
5. **Contract tests are reusable** — the same test suite certifies every adapter for a given port.

## External Systems

The following external systems are expected to be consumed through ports:

| System | Expected Port |
|---|---|
| AWS (jobs, infra) | `JobRunnerPort`, `ArtifactStorePort` |
| Jira | `IssueTrackerPort` |
| Grafana | `MetricsQueryPort`, `DashboardPort` |
| Okta | `IdentityProviderPort` |
| GitHub | `SourceControlPort` |
| Log aggregation | `LogStreamPort` |
| Secrets manager | `SecretsPort` |

Each port must have a contract test suite. See [Port Contract Template](./port-contract-template.md).
