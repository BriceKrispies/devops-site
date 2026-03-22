# Backend Documentation

This documentation set operationalizes the [Backend Constitution](./backend-constitution.md). Every document here exists to make a constitutional rule concrete, enforceable, and actionable.

## Document Map

| Document | Purpose |
|---|---|
| [Backend Constitution](./backend-constitution.md) | The governing contract. All other docs derive from this. |
| [Architecture](./architecture.md) | System shape, layers, dependency directions, capability flow. |
| [Layering Rules](./layering-rules.md) | Explicit allowed/forbidden imports per layer with analyzer assertions. |
| [Testing Strategy](./testing-strategy.md) | Test taxonomy, naming, directory layout, banned patterns. |
| [Observability Strategy](./observability-strategy.md) | Logs, metrics, traces, audit events, correlation, test assertions. |
| [Error Model](./error-model.md) | Error taxonomy, typed categories, mapping rules, response shapes. |
| [Operation Context](./operation-context.md) | Standard context model, required fields, propagation, validation. |
| [Capability Template](./capability-template.md) | Reusable template for defining a new capability slice. |
| [Port Contract Template](./port-contract-template.md) | Template for defining a port and its contract test suite. |
| [Adapter Certification](./adapter-certification.md) | What an adapter must satisfy to be accepted into the system. |
| [Configuration Strategy](./configuration-strategy.md) | Typed config, validation, secrets, startup checks, prohibited patterns. |
| [Runtime Guards](./runtime-guards.md) | Fail-fast conditions and runtime invariant checks. |
| [CI Gates](./ci-gates.md) | Required CI checks for merge, merge blockers. |
| [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md) | Analyzer categories, architecture test assertions, examples. |
| [Scaffolding Strategy](./scaffolding-strategy.md) | Approved generators for capabilities, ports, adapters, endpoints. |
| [Audit & Admin Actions](./audit-and-admin-actions.md) | Audit requirements, event schema, retention, covered actions. |
| [AI Contribution Rules](./ai-contribution-rules.md) | How AI agents must operate in this repo. |
| [Definition of Done](./definition-of-done.md) | PR and capability checklist for humans and AI. |
| [Exception Policy](./exception-policy.md) | How temporary exceptions are recorded, reviewed, and expired. |
| [Repo Enforcement Plan](./repo-enforcement-plan.md) | Constitution-to-enforcement mapping with phased rollout. |
| [Glossary](./glossary.md) | Stable term definitions used across all docs. |
| [Roadmap](./roadmap.md) | Phased adoption plan from day 0 through hardening. |

## Reading Order

### For New Contributors

1. [Backend Constitution](./backend-constitution.md)
2. [Glossary](./glossary.md)
3. [Architecture](./architecture.md)
4. [Layering Rules](./layering-rules.md)
5. [Error Model](./error-model.md)
6. [Operation Context](./operation-context.md)
7. [Testing Strategy](./testing-strategy.md)
8. [Observability Strategy](./observability-strategy.md)
9. [Capability Template](./capability-template.md)
10. [Definition of Done](./definition-of-done.md)

### For AI Coding Agents

1. [AI Contribution Rules](./ai-contribution-rules.md) — **start here, mandatory**
2. [Backend Constitution](./backend-constitution.md)
3. [Architecture](./architecture.md)
4. [Layering Rules](./layering-rules.md)
5. [Capability Template](./capability-template.md)
6. [Port Contract Template](./port-contract-template.md)
7. [Testing Strategy](./testing-strategy.md)
8. [Error Model](./error-model.md)
9. [Observability Strategy](./observability-strategy.md)
10. [Definition of Done](./definition-of-done.md)
11. [Scaffolding Strategy](./scaffolding-strategy.md)

### For Platform/Infra Engineers

1. [Backend Constitution](./backend-constitution.md)
2. [Repo Enforcement Plan](./repo-enforcement-plan.md)
3. [CI Gates](./ci-gates.md)
4. [Static Analysis & Architecture Tests](./static-analysis-and-architecture-tests.md)
5. [Runtime Guards](./runtime-guards.md)
6. [Configuration Strategy](./configuration-strategy.md)
7. [Scaffolding Strategy](./scaffolding-strategy.md)
8. [Roadmap](./roadmap.md)
