# AI Contribution Rules

> Derived from [Backend Constitution](./backend-constitution.md) §14.

## Purpose

Define how AI coding agents must operate in this repository. AI is permitted and encouraged, but constrained by the constitution and mechanical enforcement.

## Mandatory Reading Order

Before making any change, an AI agent must read:

1. **This document** — rules of engagement
2. [Backend Constitution](./backend-constitution.md) — governing contract
3. [Architecture](./architecture.md) — system shape and layers
4. [Layering Rules](./layering-rules.md) — what is allowed/forbidden per layer
5. [Capability Template](./capability-template.md) — structure of a new capability
6. [Port Contract Template](./port-contract-template.md) — structure of a port and contract tests
7. [Testing Strategy](./testing-strategy.md) — test taxonomy and banned patterns
8. [Error Model](./error-model.md) — error taxonomy and result types
9. [Observability Strategy](./observability-strategy.md) — telemetry requirements
10. [Definition of Done](./definition-of-done.md) — completion checklist

## Standard AI Workflow

Every AI-generated change must follow this workflow:

### Step 1: Read Context

- Read the constitution and relevant docs (see reading order above)
- Identify the affected capability, layer, and ports
- Read existing code in the affected area

### Step 2: Update Specs and Contracts First

- If adding a new capability: create or update spec tests **before** implementation
- If modifying a port: update contract tests **before** adapter changes
- If fixing a bug: write a failing regression test **before** the fix

### Step 3: Implement

- Follow the layering rules strictly
- Use scaffolding if creating new capabilities, ports, or adapters
- Use the standard error model — do not invent error types
- Use the standard operation context — do not bypass it
- Include observability (telemetry) in the implementation

### Step 4: Run All Checks

Before considering the change complete, verify:

- [ ] Type check passes
- [ ] Lint passes
- [ ] Architecture tests pass
- [ ] Domain spec tests pass
- [ ] Application behavior tests pass
- [ ] Port contract tests pass (if ports are affected)
- [ ] Adapter tests pass (if adapters are affected)
- [ ] Observability assertions pass

### Step 5: Summarize

Provide a summary of:
- What was changed and why
- Which layers were affected
- Which tests were added or modified
- Any risks or assumptions
- Any constitution exceptions required

## Allowed AI Actions

| Action | Allowed | Constraint |
|---|---|---|
| Generate spec tests from contracts | Yes | Tests must be meaningful, not trivial |
| Generate implementations from specs | Yes | Must pass all checks |
| Generate adapters from port definitions | Yes | Must pass contract suite |
| Generate regression tests from bug reports | Yes | Must reproduce the failure |
| Refactor internals | Yes | Must preserve behavior; all tests must still pass |
| Propose edge cases and invariants | Yes | Must be added to spec tests |
| Create new files | Yes | Must use scaffold structure |

## Prohibited AI Actions

| Action | Why |
|---|---|
| Bypassing architecture tests | Constitution violation |
| Skipping spec tests for "simple" changes | No change is too simple for specs |
| Placing vendor SDK calls in Domain or Application | Layering violation |
| Using `throw` for business outcomes | Error model violation |
| Using `console.log` for application logging | Observability violation |
| Creating untyped config access | Configuration strategy violation |
| Modifying CI pipeline to skip checks | Enforcement circumvention |
| Generating code without running checks | Unvalidated code is not progress |
| Inventing terminology not in the [Glossary](./glossary.md) | Naming drift |
| Adding dependencies without justification | Supply chain risk |

## AI Trust Model

AI-generated code is not trusted by default. Trust is established only by passing:

1. Static analyzers
2. Architecture tests
3. Spec tests
4. Contract tests
5. Observability checks

AI-generated code that passes none of the above is not progress.

## Shapes That Help AI Succeed

The repo prefers patterns that AI can follow consistently:

| Pattern | Why It Helps AI |
|---|---|
| Small, bounded capability slices | Reduced scope = fewer mistakes |
| Explicit port interfaces | Clear contract to implement against |
| Stable test templates | AI fills in specifics, not structure |
| Strong naming rules | Consistent naming = consistent generation |
| Generated scaffolds | Structure is provided, not invented |
| Narrow files with single responsibility | Less context needed per file |
| Standard result/error types | One pattern to follow everywhere |

## Enforcement

| Rule | Mechanism |
|---|---|
| AI follows workflow | PR review + CI gates |
| AI-generated code passes checks | CI pipeline (same as human code) |
| AI does not bypass scaffolds | CI structural checks |
| AI changes include tests | CI gate: new code must have tests |

There is no separate review process for AI vs human code. The same mechanical enforcement applies to both.
