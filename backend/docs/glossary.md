# Glossary

> Stable term definitions used across all backend documentation.

## Terms

| Term | Definition |
|---|---|
| **Adapter** | Concrete implementation of a port. Lives in the Adapters layer. Depends on vendor SDKs. Must pass the port's contract test suite. |
| **Application layer** | Layer containing use cases, port definitions, result types, and error taxonomy. Orchestrates domain behavior. Must not depend on adapters or vendor SDKs. |
| **Architecture test** | Automated test that verifies structural rules: dependency direction, namespace boundaries, forbidden imports. |
| **Audit event** | Durable, immutable record of a privileged or operationally meaningful action. Distinct from logs. |
| **Capability** | A bounded slice of functionality with a defined input contract, output contract, invariants, failure modes, side effects, observability, and tests. The unit of feature delivery. |
| **Causation ID** | Identifier of the event or operation that directly triggered the current operation. Used for causal tracing. |
| **Contract test** | Reusable test suite that defines the behavioral contract of a port. Any adapter implementing the port must pass the suite. |
| **Correlation ID** | Unique identifier that propagates through an entire request/workflow chain, linking all related operations. |
| **Domain layer** | Innermost layer containing entities, value objects, domain services, invariants, and pure business rules. Has no outward dependencies. |
| **Error taxonomy** | The fixed set of classified error categories (validation, authorization, not found, conflict, etc.) used across the system. |
| **Exception (policy)** | A documented, time-bounded, owned deviation from a constitutional rule. Not to be confused with programming exceptions. |
| **Fail fast** | Design principle: detect invalid state as early as possible and refuse to proceed, rather than silently continuing. |
| **Host layer** | Outermost layer containing DI composition, startup, middleware, config binding, endpoint registration. No business logic. |
| **Invariant** | A condition that must always be true for a domain entity or operation. Violations are fatal. |
| **Mutation testing** | Testing technique that introduces small changes (mutations) to code and verifies that tests catch them. Exposes weak tests. |
| **Operation context** | Standard context object carried by every command, query, and background operation. Contains correlation ID, operation name, actor, tenant, and timestamp. |
| **Port** | An interface defined in the Application layer that abstracts an external dependency. Adapters implement ports. |
| **Result type** | A typed union (`Result<T, E>`) used to represent success or failure outcomes. Preferred over thrown exceptions for business flow. |
| **Runtime guard** | A check that runs at startup or request time and fails fast if preconditions are not met. |
| **Scaffold** | An approved generator that creates the correct file structure, test skeletons, and observability hooks for a new artifact. |
| **Spec test** | An executable specification that defines expected behavior. The primary proof that code is correct. |
| **Structured log** | A log entry emitted as key-value pairs (JSON or equivalent), not a plain string. Required for all application-level logging. |
| **Transport shell** | An endpoint, job runner, or queue consumer in the Host layer. Thin wrapper: parse input, invoke use case, map output. Contains no business logic. |
| **Vendor SDK** | An external library or SDK for communicating with a third-party system (AWS SDK, Jira client, Okta SDK, etc.). Allowed only in the Adapters layer. |

## Naming Conventions

| Convention | Rule |
|---|---|
| Capability names | PascalCase, verb-noun (e.g., `RerunJob`, `FetchDeploymentStatus`) |
| Port names | PascalCase, suffixed with `Port` (e.g., `JobRunnerPort`, `IssueTrackerPort`) |
| Adapter names | PascalCase, prefixed with vendor name (e.g., `AwsJobRunnerAdapter`, `JiraIssueTrackerAdapter`) |
| Error codes | UPPER_SNAKE_CASE (e.g., `VALIDATION`, `NOT_FOUND`, `DEPENDENCY_UNAVAILABLE`) |
| Test files | `{Subject}.{type}.ts` where type is `spec`, `contract`, `adapter.spec`, `regression.spec`, `arch.spec` |
| Operation names | PascalCase, matching capability name |
| Metric names | dot-separated lowercase (e.g., `capability.invocations`, `external.latency`) |
