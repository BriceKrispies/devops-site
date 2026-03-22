# Definition of Done

> Derived from [Backend Constitution](./backend-constitution.md) §15.

## Purpose

Practical checklist for determining whether a capability or PR is complete. Usable by both humans and AI agents.

## Capability Done Checklist

A capability is done only when **every** applicable item is checked:

### Behavior

- [ ] Behavior is specified by executable tests (domain spec tests + application behavior tests)
- [ ] Domain invariants are covered by domain spec tests
- [ ] Edge cases and boundary conditions are tested
- [ ] Regression cases are captured where relevant

### Contracts

- [ ] Port contracts are defined for all external dependencies
- [ ] Port contract test suite exists and passes
- [ ] Adapter implementations pass the full contract suite
- [ ] Input contract is typed and validated
- [ ] Output contract is typed

### Architecture

- [ ] Implementation passes architecture tests (dependency direction)
- [ ] No forbidden imports (vendor SDKs in domain/application, framework types in domain)
- [ ] Static analyzers pass (determinism, structured logging, result types)
- [ ] Code is in the correct layer per [Layering Rules](./layering-rules.md)

### Observability

- [ ] Top-level trace span is emitted with correct operation name
- [ ] Structured log on completion includes required fields
- [ ] Success/failure metric counter is incremented
- [ ] Latency metric is recorded
- [ ] External call spans are emitted for each port call
- [ ] Observability behavior is asserted in tests

### Errors

- [ ] Failures are typed and classified per [Error Model](./error-model.md)
- [ ] Vendor errors are mapped to internal taxonomy in adapters
- [ ] Error codes are from the standard taxonomy (no ad-hoc codes)
- [ ] Error paths are tested

### Configuration

- [ ] Configuration is typed and validated at startup
- [ ] Secrets use references, not raw values
- [ ] No magic-string config access in domain or application

### Security (Constitution §14)

- [ ] CapabilityDescriptor declared with authorization metadata
- [ ] Descriptor registered in CapabilityRegistry at startup
- [ ] Authentication requirement declared (default: required)
- [ ] Required permissions declared using stable internal names
- [ ] Authorization enforced through IAuthorizationService in handler
- [ ] Unauthenticated/forbidden behavior tested
- [ ] Privileged capabilities marked as `IsPrivileged = true`
- [ ] Handler added to SecurityMetadataTests.AllDescriptors

### Audit

- [ ] Privileged actions emit audit events (if applicable)
- [ ] Privileged descriptors require audit (`RequiresAudit = true`)
- [ ] Audit events include required fields
- [ ] Audit emission is tested

### Process

- [ ] Scaffolding was used for new artifacts
- [ ] PR passes all CI gates
- [ ] Any constitution exceptions are documented per [Exception Policy](./exception-policy.md)

## PR Checklist (Short Form)

For quick PR review, verify:

- [ ] All CI gates pass (architecture, analyzers, specs, contracts, observability)
- [ ] New behavior has spec tests
- [ ] New ports have contract tests
- [ ] New adapters pass contract suite
- [ ] Errors use standard taxonomy
- [ ] Observability is emitted and tested
- [ ] No layering violations
- [ ] Authorization metadata declared and registered
- [ ] No TODO/FIXME without exception record

## What "Done" Does NOT Mean

| Condition | Is It Done? |
|---|---|
| Code compiles | No |
| Code passes lint | No |
| Code works manually | No |
| High line coverage | No |
| Code is deployed | Not necessarily — done means the above checklist is satisfied |

The code is considered correct only when specs, architecture constraints, observability, and contracts all pass.
