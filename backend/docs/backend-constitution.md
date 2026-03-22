# Backend Constitution

## 1. Purpose

This backend exists to produce systems whose behavior is defined by executable specifications, whose implementations are replaceable, whose operations are observable, and whose constraints are mechanically enforced from day one.

The backend is not considered correct because it compiles.
It is considered correct only when:

- its executable specifications pass
- its architectural constraints pass
- its observability requirements pass
- its contracts with external systems pass

Implementation is subordinate to contract.

## 2. Non-Negotiable Principles

### 2.1 Spec over implementation

Every meaningful behavior must be described by tests before it is trusted.

### 2.2 Core logic must be replaceable

Business behavior must not depend on concrete vendors, frameworks, transport layers, storage engines, or SDKs.

### 2.3 Observability is part of correctness

A use case is incomplete if it cannot be traced, logged, measured, and audited where appropriate.

### 2.4 Every rule must be enforceable

If a rule cannot be checked automatically, it is not yet a real rule.

### 2.5 AI is constrained by the constitution

AI may generate code, tests, adapters, and refactors, but only within repo-enforced boundaries and passing checks.

## 3. System Shape

The codebase is divided into these layers only:

### 3.1 Domain

Contains:

- domain entities
- value objects
- domain services
- invariants
- pure business rules

Must not contain:

- database code
- HTTP code
- cloud SDK calls
- file system access
- time access without abstraction
- randomness without abstraction
- logging implementation details

### 3.2 Application

Contains:

- use cases
- commands/queries
- orchestration of domain behavior
- port definitions
- result types
- error taxonomy
- transaction boundaries where abstracted

Must not contain:

- vendor SDKs
- SQL
- HTTP clients
- framework-specific controller logic
- business rules hidden in handlers

### 3.3 Adapters

Contains:

- AWS/Jira/Grafana/Okta/GitHub integrations
- database repositories
- cache implementations
- queue implementations
- HTTP transport adapters
- telemetry plumbing

May depend on external libraries and SDKs.

### 3.4 Host

Contains:

- DI composition
- app startup
- middleware
- config binding
- endpoint registration
- runtime wiring

No business rules are allowed here.

## 4. Dependency Law

Allowed dependency direction is:

```
Host -> Adapters -> Application -> Domain
```

Additional allowed references:

- Host -> Application
- Host -> Domain
- Adapters -> Domain

Forbidden:

- Domain -> anything outside Domain
- Application -> Adapters
- Application -> Host
- Domain -> framework packages
- Domain/Application -> vendor SDKs

This must be enforced by architecture tests and project reference rules.

## 5. Capability Rule

Every new capability must be created as a bounded slice with all of the following:

- capability name
- command/query input contract
- output contract
- invariants
- failure modes
- side effects
- observability requirements
- executable spec tests
- adapter contract tests where external systems are involved

A capability is not complete if any of the above is missing.

## 6. Testing Constitution

### 6.1 Tests are first-class system artifacts

Tests are not auxiliary. They are the executable definition of behavior.

### 6.2 Required test layers

#### A. Domain specification tests

Must verify:

- invariants
- business rules
- state transitions
- edge cases
- regression cases

These tests must not mock internal domain behavior.

#### B. Application behavior tests

Must verify:

- use case behavior
- orchestration across ports
- error handling
- idempotency
- policy enforcement

These tests may fake ports, but must not assert implementation trivia.

#### C. Port contract tests

Every external port must have a reusable contract suite that any adapter implementation must pass.

Example targets:

- issue tracker port
- metrics port
- secrets/config port
- job runner port
- log retrieval port
- artifact storage port

#### D. Adapter tests

Adapters must be tested against:

- contract suite
- serialization boundaries
- retries/timeouts
- failure classification
- telemetry emission

#### E. Regression tests

Every production bug must begin with a failing test before the fix is accepted.

### 6.3 Forbidden test patterns

The following are banned unless explicitly justified:

- testing private methods
- tests that assert call count as primary correctness proof
- mock-heavy tests with no behavior meaning
- asserting implementation details instead of contract
- giant end-to-end tests used as substitute for missing specs

### 6.4 Coverage rule

Coverage is a lagging indicator, not the target.

The target is:

- behavioral coverage
- invariant coverage
- contract coverage
- failure mode coverage

Line coverage may be tracked, but no code is considered safe merely because it is executed by tests.

### 6.5 Mutation pressure

The repo should adopt mutation testing or equivalent pressure for critical core logic so weak tests are exposed early.

## 7. Observability Constitution

Observability is required for every meaningful operation.

### 7.1 Every operation must carry context

Every command, query, and background operation must carry:

- correlation id
- causation id where applicable
- actor or service identity where applicable
- tenant or scope identifier where applicable
- operation name

Missing context is a failure.

### 7.2 Structured logs only

Logs must be structured. String-only logs are not allowed for application behavior.

Every important log must include:

- operation name
- correlation id
- result status
- latency if applicable
- external dependency target if applicable
- classified error code on failure

### 7.3 Tracing

Every external call and every top-level capability execution must emit spans.

### 7.4 Metrics

Every important capability must emit:

- invocation count
- success/failure count
- latency
- retry count where applicable

### 7.5 Audit trail

Every privileged or operationally meaningful action must emit an audit record.

Examples:

- rerunning jobs
- changing flags
- mutating access
- replaying workflows
- administrative overrides
- manual remediation actions

### 7.6 Observability tests

Critical capabilities must assert observability behavior in tests, not just business output.

## 8. Determinism and Side-Effect Law

Anything nondeterministic must be abstracted behind a port or service boundary.

This includes:

- current time
- random values
- GUID/ID generation where behavior depends on it
- environment access
- network access
- file system access
- process execution

Direct use in Domain and Application is forbidden unless explicitly wrapped in approved abstractions.

## 9. Error Constitution

Errors must be explicit, typed, and classified.

### 9.1 No opaque exception-driven business flow

Business outcomes must not rely on arbitrary thrown exceptions as normal control flow.

### 9.2 Error taxonomy required

Each capability must classify errors into stable categories such as:

- validation
- authorization
- not found
- conflict
- rate limited
- dependency unavailable
- timeout
- transient dependency failure
- permanent dependency failure
- internal invariant violation

### 9.3 Errors must be observable

Errors must emit:

- stable code
- severity
- operation name
- dependency if relevant
- correlation id

## 10. External Integration Constitution

All external systems must be consumed through explicit ports.

Examples:

- JiraPort
- AwsJobRunnerPort
- GrafanaQueryPort
- IdentityProviderPort
- SourceControlPort
- ArtifactStorePort
- LogStreamPort

Rules:

- external SDKs are adapter-only
- no direct SDK calls in Domain or Application
- every port has a contract suite
- every adapter must map vendor-specific failures into internal error taxonomy
- retries, backoff, timeout, and cancellation behavior must be standardized

## 11. Configuration Constitution

Configuration must be:

- typed
- validated at startup
- environment-specific but shape-stable
- fail-fast on invalid values
- never accessed as arbitrary string lookups in core logic

Secrets must never be read directly from arbitrary locations in Domain or Application.

## 12. Endpoint and Job Constitution

Endpoints, consumers, schedulers, and jobs are transport shells only.

They may:

- parse input
- invoke application use cases
- map outputs to transport responses

They may not:

- contain business rules
- talk directly to vendor SDKs
- build SQL
- perform hidden orchestration
- emit unstructured logs

## 13. Mechanical Enforcement Rules

### 13.1 Static enforcement

Must exist from day one:

- project reference restrictions
- analyzers for forbidden namespaces/usings
- analyzers for banned APIs in forbidden layers
- style/lint rules for structured logging and result/error handling

### 13.2 Architecture tests

Must verify:

- dependency direction
- namespace boundaries
- adapter-only vendor references
- no business logic in transport/host layers
- no concrete infrastructure types referenced by Application or Domain

### 13.3 Scaffolding enforcement

New capabilities must be created only through approved templates/scaffolds that generate:

- capability skeleton
- spec tests
- port definitions
- observability hooks
- contract test placeholders

### 13.4 CI gates

A merge must fail when any of the following is true:

- architecture tests fail
- analyzers fail
- spec tests fail
- contract tests fail
- observability assertions fail
- new capability lacks required tests
- changed public contract lacks updated fixtures/specs

### 13.5 Runtime guards

The application must fail fast for:

- invalid config
- missing telemetry context where required
- duplicate capability registration
- missing adapter bindings
- invalid auth/tenant context
- invariant violations

## 14. Security Constitution

### 14.1 Deny by default

All endpoints and capabilities require authenticated user context by default. Anonymous/public access must be explicit, rare, and declared through a CapabilityDescriptor with `RequiresAuthentication = false`. Missing authorization metadata must be treated as an error, not as open access.

### 14.2 Capability-level authorization declaration

Every application capability must explicitly declare:

- whether authentication is required (default: yes)
- required permission(s) using stable internal permission names
- whether the action is privileged/mutating
- whether audit is required (mandatory for privileged actions)

If a capability is missing its CapabilityDescriptor, startup validation and architecture tests must fail.

### 14.3 Backend enforcement is the source of truth

Frontend visibility rules are only UX. Real authorization must be enforced in the backend application pipeline. No endpoint may rely on frontend hiding for security.

### 14.4 Privileged action classification

Mutating, admin, and operationally dangerous actions must:

- be classified as `IsPrivileged = true` in their descriptor
- require explicit permission
- require audit metadata (`RequiresAudit = true`)
- emit audit events through the AuditPort

### 14.5 Permission model

Permissions are stable internal identifiers (e.g., `workitem:read`), not raw identity provider claim names. The host layer maps external identity claims to internal permissions. Domain and Application layers never reference identity provider concepts.

### 14.6 Enforcement mechanisms

| Rule | Mechanism |
|---|---|
| Every handler has a Descriptor | Architecture test: reflection scan |
| All descriptors are registered | Startup validation: fail-fast |
| Privileged requires audit | CapabilityDescriptor.Validate() |
| Unregistered capability denied | AuthorizationService returns MissingDescriptor |
| No scattered auth checks | Architecture test: no raw claim access in Application/Domain |

## 15. AI Usage Constitution

AI is permitted and encouraged, but only under hard constraints.

AI may be used to:

- generate specs from written contracts
- generate implementations to satisfy specs
- generate adapters from port definitions
- generate regression tests from bug reports
- refactor internals while preserving behavior
- propose edge cases and invariant suites

AI may not be trusted by default.

Every AI-generated change must satisfy:

- analyzers
- architecture tests
- spec tests
- contract tests
- observability checks

AI-generated code that passes none of the above is not progress.

The repo must prefer shapes that AI can follow consistently:

- small slices
- explicit ports
- stable test templates
- strong naming rules
- generated scaffolds
- narrow files with single responsibility

## 16. Definition of Done

A capability is done only when:

- behavior is specified by executable tests
- domain invariants are covered
- port contracts are defined and tested
- implementation passes architecture rules
- observability is emitted and asserted
- failures are typed and classified
- config is typed and validated
- privileged actions are auditable if applicable
- authorization metadata declared via CapabilityDescriptor
- authorization enforced through standard path
- regressions have been captured where relevant

If the code works but these conditions are not met, it is not done.

## 17. Initial Enforcement Backlog

These are the first mandatory enforcement mechanisms to build immediately:

### Phase 1

- strict repo/project layering
- architecture test suite
- forbidden dependency analyzers
- capability scaffold generator
- standard result/error model
- operation context model
- structured logging pipeline
- base spec test template
- base port contract test template

### Phase 2

- CI merge gates for missing spec coverage
- observability assertion helpers
- adapter certification suite
- startup config validation framework
- audit event infrastructure

### Phase 3

- mutation testing for critical core modules
- generated PR policy checks
- richer static rules for anti-pattern detection
- AI prompt templates tied to repo constitution

## 18. Default Engineering Rules

These rules apply unless an exception is explicitly approved:

- no hidden side effects
- no framework types in Domain
- no vendor SDKs outside Adapters
- no business logic in controllers/jobs/consumers
- no new capability without spec tests
- no new adapter without contract tests
- no privileged mutation without audit events
- no important operation without telemetry
- no config by magic strings
- no unclassified failure paths
- no exceptions as undocumented behavior
- no bypassing scaffolds for new slices

## 19. Exception Policy

Exceptions to this constitution must be:

- documented
- time-bounded
- justified
- visible in CI or issue tracking
- removed intentionally

"Temporary" without an owner or expiry is not allowed.

## 20. Final Rule

When a choice exists between:

- speed through undocumented implementation
- speed through enforceable contracts and scaffolds

the second option wins.

Fast is good.
Fast and governable is mandatory.
