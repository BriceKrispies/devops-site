# Backend — Agent Instructions

## Governing Documents

Before making any change, read these in order:
1. `docs/backend-constitution.md` — the governing contract
2. `docs/architecture.md` — system shape and layers
3. `docs/layering-rules.md` — allowed/forbidden imports per layer
4. `docs/error-model.md` — error taxonomy
5. `docs/testing-strategy.md` — test taxonomy
6. `docs/definition-of-done.md` — completion checklist

## Tech Stack

- .NET 8 (C#)
- xUnit for tests
- NetArchTest for architecture enforcement
- Minimal APIs (ASP.NET Core) for Host layer

## Solution Structure

```
build/
  _build.csproj              — NUKE build orchestration (BuildAll, BuildProject, TestUnit, CoverageReport)
src/
  DevOpsSite.Domain/        — Entities, value objects, invariants. NO external dependencies.
  DevOpsSite.Application/   — Use cases, ports, result/error types. Depends on Domain only.
  DevOpsSite.Adapters/      — Port implementations, vendor integrations. Depends on Application + Domain.
  DevOpsSite.Host/          — DI, config, endpoints, middleware. Composes the API host.
  DevOpsSite.Worker/        — Background worker host. DI, config, hosted services. Wiring only.
tests/
  DevOpsSite.Domain.Tests/       — Domain spec tests
  DevOpsSite.Application.Tests/  — Application behavior + observability assertion tests
  DevOpsSite.Adapters.Tests/     — Adapter tests (run contract suites)
  DevOpsSite.Architecture.Tests/ — Dependency direction + layer content tests
  DevOpsSite.Contracts.Tests/    — Abstract port contract test suites
  DevOpsSite.Worker.Tests/       — Worker hosted service behavior tests
```

## Dependency Law

```
Host/Worker -> Adapters -> Application -> Domain
```
Domain must NEVER reference Application, Adapters, Host, or Worker.
Application must NEVER reference Adapters, Host, or Worker.
Adapters must NEVER reference Host or Worker.

## Commands

```bash
# NUKE orchestration (preferred)
dotnet run --project build/_build.csproj -- BuildAll                                  # Build entire solution (Release)
dotnet run --project build/_build.csproj -- BuildAll --configuration Debug            # Build in Debug
dotnet run --project build/_build.csproj -- BuildProject --project-name DevOpsSite.Domain  # Build single project
dotnet run --project build/_build.csproj -- TestUnit                                  # Run all tests
dotnet run --project build/_build.csproj -- CoverageReport                            # Tests + coverage report + percentage

# Direct dotnet commands
dotnet build DevOpsSite.sln          # Build all (API host + worker + tests)
dotnet test DevOpsSite.sln           # Run all tests
dotnet run --project src/DevOpsSite.Host          # Run API host
dotnet run --project src/DevOpsSite.Worker        # Run background worker

# Legacy CI script (superseded by NUKE CoverageReport)
bash scripts/ci.sh                   # Full CI: build + test + coverage
bash scripts/ci.sh Debug             # Same, Debug configuration
```

## NUKE Build Orchestration

`build/_build.csproj` contains the NUKE build with these targets:

| Target | Description |
|---|---|
| `BuildAll` | Restore + build entire solution. Release by default. Default target. |
| `BuildProject` | Build a single project. Requires `--project-name`. Fails clearly if not found. |
| `TestUnit` | Run all unit tests. Depends on BuildAll. Quiet except failures/summary. |
| `CoverageReport` | Run tests with coverage. Produces `artifacts/coverage/merged.cobertura.xml` + HTML report. Prints aggregate coverage percentage. |

Parameters:
- `--configuration` — `Release` (default) or `Debug`
- `--project-name` — project name for `BuildProject` (e.g., `DevOpsSite.Domain`)

The NUKE build uses the local `dotnet-reportgenerator-globaltool` from `.config/dotnet-tools.json` for coverage merging.

## CI Script (Legacy)

`scripts/ci.sh` is the original CI entrypoint. It:
- Restores, builds (Release), runs all tests with coverage
- Prints only build errors and test failures
- Produces `artifacts/coverage/merged.cobertura.xml` (machine-readable)
- Produces `artifacts/coverage/report/` (HTML report)
- Prints final: build status, test status, coverage percentage
- Exits non-zero on any failure

The NUKE `CoverageReport` target supersedes this script with the same outputs.

## Scaffolds

```bash
bash scaffolds/scaffold.sh capability <Name>          # New capability
bash scaffolds/scaffold.sh port <Name>                # New port + contract test
bash scaffolds/scaffold.sh adapter <Name> <PortName>  # New adapter
bash scaffolds/scaffold.sh endpoint <Name> <Path>     # New endpoint shell
bash scaffolds/scaffold.sh regression <BugId>         # New regression test
```

## Adapter Certification (MANDATORY)

Every adapter that talks to an external system must be certified before merge. Use `bash scaffolds/scaffold.sh adapter <Name> <PortName>` which generates both contract test and certification test stubs.

Required certification coverage:
- Port contract suite passes (abstract test base in `tests/DevOpsSite.Contracts.Tests/`)
- HTTP failure modes tested with stubs: 404, 401/403, 429, 5xx, timeout, cancellation, network error, malformed payload
- Telemetry asserted: spans with `externalTarget`, `external.calls` counter, error logs with dependency name
- No vendor type leakage through port interface
- Typed config validated at startup

Reference: `src/DevOpsSite.Adapters/Jira/` and `tests/DevOpsSite.Adapters.Tests/Jira/`
Full spec: `docs/adapter-certification.md`

## Security Model (Deny by Default)

Every capability requires authenticated access by default. Public endpoints must be explicitly declared.

### Adding a new protected capability

1. Declare a `public static readonly CapabilityDescriptor Descriptor` on the handler:
   ```csharp
   public static readonly CapabilityDescriptor Descriptor = new()
   {
       OperationName = OperationName,
       RequiresAuthentication = true,        // default, can omit
       RequiredPermissions = [Permission.WellKnown.YourPermission],
       IsPrivileged = false,                 // true for mutating/admin
       RequiresAudit = false,                // must be true if IsPrivileged
       Description = "What this capability does."
   };
   ```
2. Add `IAuthorizationService` to handler constructor
3. Call `_authz.Evaluate(OperationName, ctx)` before business logic
4. Map `AuthorizationResult` failures to `AppError.Unauthenticated()` or `AppError.Forbidden()`
5. Register descriptor in `ServiceRegistration.AddBackendServices()`
6. Add to `SecurityMetadataTests.AllDescriptors`
7. Add auth tests (unauthenticated, forbidden, success with permissions)

### Allowing public/anonymous access

Set `RequiresAuthentication = false` in the descriptor. Do NOT add permissions.

### Privileged/mutating capabilities

Set `IsPrivileged = true` and `RequiresAudit = true`. Emit audit events via `IAuditPort`.

### What happens if you forget

- Missing `Descriptor` field → `SecurityMetadataTests` fails
- Missing registry entry → `SecurityMetadataTests` fails + startup validation fails
- Privileged without audit → `CapabilityDescriptor.Validate()` fails
- Unregistered operation at runtime → `AuthorizationService` returns `MissingDescriptor` (denied)

Reference: `src/DevOpsSite.Application/Authorization/` for all primitives.
Full spec: `docs/backend-constitution.md` §14.

## Capability Shell Model

The backend uses an extended `CapabilityDescriptor` model to classify all operational capabilities — both implemented and planned. This prepares the system for future AWS operational features (SQS queues, RDS databases, CloudWatch logs) without requiring the full AWS capability matrix upfront.

### Capability metadata fields

Every capability declares:
- **OperationName** — unique identifier
- **Category** — functional group (`Traces`, `ServiceHealth`, `WorkItems`, `Queues`, `Databases`, `Logs`, `Admin`)
- **RiskLevel** — `Low`, `Medium`, `High`, `Critical`
- **ExecutionMode** — `Synchronous` or `Asynchronous`
- **ImplementationStatus** — `Planned`, `Stub`, `Ready`, `Disabled`
- **ExecutionProfile** — `Default`, `ReadOnly`, `QueueOperator`, `DatabaseOperator`, `Admin`
- Plus existing auth fields: `RequiresAuthentication`, `RequiredPermissions`, `IsPrivileged`, `RequiresAudit`

### OperationalCapabilityCatalog

`src/DevOpsSite.Application/Authorization/OperationalCapabilityCatalog.cs` is the central catalog of ALL capabilities. It provides:
- `All` — every capability (implemented + planned)
- `Implemented` — only `Status=Ready` capabilities (registered in CapabilityRegistry at startup)
- `Planned` — reserved slots with full metadata but no handler yet
- `ByCategory(category)` — filter by functional category
- `GetByOperationName(name)` — lookup by operation name

### Execution profile seam

`ExecutionProfile` is internal metadata for future AWS IAM role mapping. Today it classifies what kind of infrastructure access a capability needs. When AWS integration is implemented, the adapter layer will map profiles to IAM role assumption. Do NOT couple these to raw AWS IAM policy strings.

### Reserved AWS capability entries

| Operation | Category | Risk | Mode | Profile | Status |
|---|---|---|---|---|---|
| `QueuesRead` | Queues | Low | Sync | ReadOnly | Planned |
| `QueuesRedriveDlq` | Queues | High | Async | QueueOperator | Planned |
| `DatabasesRead` | Databases | Low | Sync | ReadOnly | Planned |
| `DatabasesCloneNonProd` | Databases | Critical | Async | DatabaseOperator | Planned |
| `LogsRead` | Logs | Low | Sync | ReadOnly | Planned |

### Adding a new AWS operational capability

1. Add a `Permission.WellKnown` entry for the capability
2. Add a `CapabilityDescriptor` entry in `OperationalCapabilityCatalog` with all metadata
3. When implementing: create a handler with `Status = ImplementationStatus.Ready`
4. Register the handler's Descriptor in `ServiceRegistration`
5. Add to `SecurityMetadataTests.AllDescriptors`
6. Create ports/adapters for AWS integration in the adapter layer
7. The `ExecutionProfile` drives which AWS credentials/role the adapter uses

### Validation rules enforced by tests

- High/Critical risk must require audit and authentication
- ReadOnly profile must be Low risk
- Operator profiles must be privileged
- Every catalog entry must be valid, unique, described, authenticated, and permissioned
- Implemented handlers must match their catalog entries

### Key locations

- Enums: `src/DevOpsSite.Application/Authorization/` (`CapabilityCategory`, `RiskLevel`, `ExecutionMode`, `ImplementationStatus`, `ExecutionProfile`)
- Catalog: `src/DevOpsSite.Application/Authorization/OperationalCapabilityCatalog.cs`
- Extended descriptor: `src/DevOpsSite.Application/Authorization/CapabilityDescriptor.cs`
- Permissions: `src/DevOpsSite.Application/Authorization/Permission.cs`
- Tests: `tests/DevOpsSite.Architecture.Tests/CapabilityCatalogTests.cs`

## Trace Store

Normalized trace events are stored through `ITraceStorePort` (Application/Ports). The backing store is selected via `TraceStore:Provider` in config. Currently only `InMemory` is supported.

### Key locations

- Domain model: `src/DevOpsSite.Domain/Entities/TraceEvent.cs`, `ValueObjects/TraceEventId.cs`, `ValueObjects/TraceEventType.cs`
- Port: `src/DevOpsSite.Application/Ports/ITraceStorePort.cs`
- Query model: `src/DevOpsSite.Application/Queries/TraceQuery.cs`
- Handlers: `src/DevOpsSite.Application/UseCases/AddTraceEvents.cs`, `QueryTraceEvents.cs`
- In-memory adapter: `src/DevOpsSite.Adapters/TraceStore/InMemoryTraceStoreAdapter.cs`
- Config: `src/DevOpsSite.Adapters/Configuration/TraceStoreConfig.cs`
- Routes: `src/DevOpsSite.Host/Routes/TraceRoutes.cs`
- Contract tests: `tests/DevOpsSite.Contracts.Tests/TraceStore/TraceStorePortContractTests.cs`

### Adding a durable backing store

1. Create a new adapter class implementing `ITraceStorePort` in `src/DevOpsSite.Adapters/TraceStore/`
2. Add a config class (e.g., `PostgresTraceStoreConfig`) with connection details
3. Register the adapter in `ServiceRegistration` under a new `Provider` value (e.g., `"Postgres"`)
4. Run the abstract contract test suite against the new adapter
5. Storage details must NOT leak into Application or Domain — the port speaks domain language only

### Non-interface types in Application.Ports

The architecture test `LayerContentTests` enforces that all types in `Application.Ports` namespace are interfaces. Query/filter DTOs used by ports go in `Application.Queries` namespace instead.

## Worker Host

The worker (`DevOpsSite.Worker`) is a separate .NET 8 host process for background ingestion, refresh, and correlation work. It follows the same constitution as the API host.

### Why it exists

- Background processing should not be buried inside the API host
- Worker loops invoke application capabilities, not contain business logic
- The worker is independently runnable, observable, and testable

### How it differs from the API host

| Concern | API Host (`DevOpsSite.Host`) | Worker (`DevOpsSite.Worker`) |
|---|---|---|
| Entry point | HTTP endpoints (Minimal APIs) | BackgroundService hosted services |
| Trigger | HTTP requests | Timer/poll loops |
| Operation source | `OperationSource.HttpRequest` | `OperationSource.BackgroundJob` |
| Actor | User from auth token | Service identity (e.g., `worker:trace-ingestion`) |

### Running locally

```bash
dotnet run --project src/DevOpsSite.Worker
```

Config in `src/DevOpsSite.Worker/appsettings.json`. Override via environment variables or `appsettings.Development.json`.

### Key locations

- Entry point: `src/DevOpsSite.Worker/Program.cs`
- DI composition: `src/DevOpsSite.Worker/Composition/WorkerServiceRegistration.cs`
- Config: `src/DevOpsSite.Worker/Configuration/TraceIngestionConfig.cs`
- Background service: `src/DevOpsSite.Worker/Services/TraceIngestionService.cs`
- Tests: `tests/DevOpsSite.Worker.Tests/`

### Current worker responsibilities

| Service | What it does | Capability invoked |
|---|---|---|
| `TraceIngestionService` | Polls `ITraceIngestionSourcePort` for pending events, stores them via `IngestTraceEventsHandler` | `IngestTraceEvents` |

### Adding a new background responsibility

1. Create a new `BackgroundService` in `src/DevOpsSite.Worker/Services/`
2. The service must: create `OperationContext` with service identity, invoke an application use case, handle results
3. Add typed config in `Configuration/` with enabled flag and poll interval
4. Register in `WorkerServiceRegistration` (conditionally, based on `Enabled` flag)
5. Add behavior tests in `tests/DevOpsSite.Worker.Tests/`
6. **Do not put business logic in the service** — it is a transport shell, same as an API route

### Future expansion points (not yet implemented)

- Source polling (webhook follow-up, external queue consumption)
- Correlation/enrichment of stored trace events
- Retention cleanup for old trace events
- Snapshot refresh for service health or work items
- Additional ingestion source adapters (SQS, file system, etc.)

## Local Development Auth

The backend supports a `DevelopmentBypass` auth mode for local development. This injects a local persona as the authenticated actor, producing the same normalized `ActorIdentity` + `Permissions` the rest of the system expects.

### Auth modes

| Mode | When | What it does |
|---|---|---|
| `DevelopmentBypass` | Local development only | Injects local persona identity into every request |
| `Oidc` | Production (future) | Real Okta/OIDC token validation (not yet implemented) |

### Hard production guard

If `Auth:Mode` is `DevelopmentBypass` and the environment is NOT `Development`, **startup fails immediately** with a clear error. This is enforced in `ValidateCapabilityRegistry()` and tested in `DevelopmentAuthGuardTests`.

### Local personas

| Persona | Actor ID | Permissions |
|---|---|---|
| `viewer` | `dev:viewer` | `servicehealth:read`, `workitem:read`, `traceevents:read` |
| `operator` | `dev:operator` | All viewer + `traceevents:write`, `traceevents:ingest` |
| `admin` | `dev:admin` | All permissions |

### Switching personas

Set `Auth:ActivePersona` in config or environment variable:
```bash
# In appsettings.Development.json
"Auth": { "Mode": "DevelopmentBypass", "ActivePersona": "admin" }

# Or via environment variable
Auth__ActivePersona=admin dotnet run --project src/DevOpsSite.Host
```

### Key locations

- Config: `src/DevOpsSite.Adapters/Configuration/AuthConfig.cs`
- Personas: `src/DevOpsSite.Host/Authentication/DevPersonas.cs`
- Middleware: `src/DevOpsSite.Host/Authentication/DevelopmentAuthMiddleware.cs`
- Guard: `src/DevOpsSite.Host/Composition/ServiceRegistration.cs` (in `ValidateCapabilityRegistry`)
- Tests: `tests/DevOpsSite.Architecture.Tests/DevelopmentAuthGuardTests.cs`

### Future real Okta/OIDC integration

When adding real OIDC:
1. Add OIDC middleware that validates tokens and extracts claims
2. Map external claims/roles to internal `Permission` objects
3. Populate `ActorIdentity` + `Permissions` on `OperationContext` (same shape as dev auth)
4. Set `Auth:Mode` to `Oidc` in production config
5. The existing `AuthorizationService` + `CapabilityDescriptor` model remains unchanged

## Local Container Stack (Podman)

### Quick start

```bash
# From repo root (c:/dev/devops-site/)
podman compose up --build          # Build and start all services
podman compose down                # Stop all services
podman compose logs -f             # Follow logs
podman compose logs -f api         # Follow API logs only
```

### Services

| Service | URL | Description |
|---|---|---|
| `frontend` | http://localhost:8080 | Nginx serving static frontend, proxying /api/ to backend |
| `api` | http://localhost:5000 | Backend API with DevelopmentBypass auth |
| `worker` | — | Background ingestion worker |
| `redis` | localhost:6379 | Local Redis |

### Switching persona in containers

Edit `compose.yaml` and change `Auth__ActivePersona`:
```yaml
api:
  environment:
    Auth__ActivePersona: admin   # viewer, operator, or admin
```

### Files

- `compose.yaml` — Podman-compatible compose file (repo root)
- `backend/Containerfile.api` — Backend API container
- `backend/Containerfile.worker` — Worker container
- `frontend/Containerfile` — Frontend container (nginx)
- `frontend/nginx.conf` — Nginx config with API proxy

## Rules for AI Agents

1. Use scaffolds for new capabilities, ports, adapters, endpoints.
2. Write spec tests BEFORE implementation.
3. All use cases return `Result<T>`, never throw for business flow.
4. All use cases accept `OperationContext` and emit telemetry.
5. All errors use `AppError` with codes from `ErrorCode` enum.
6. No vendor SDKs in Domain or Application.
7. No `DateTime.Now`/`DateTimeOffset.UtcNow` in Domain or Application — use `IClockPort`.
8. Run `dotnet test` after every change. All tests must pass.
9. Architecture tests enforce layering. If they fail, your change violates the constitution.
10. Every adapter must pass certification (see Adapter Certification above). No exceptions.
11. Every capability must declare a `CapabilityDescriptor` and register it. No exceptions.
12. Every capability handler must inject `IAuthorizationService` and evaluate before business logic.
13. Do not scatter raw claim/role checks in handlers. Use `Permission` and `IAuthorizationService`.
