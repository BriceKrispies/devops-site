# Static Analysis & Architecture Tests

> Derived from [Backend Constitution](./backend-constitution.md) §13.1–13.2.

## Purpose

Define the analyzer categories and architecture test categories that must exist to mechanically enforce constitutional rules.

## Static Analyzer Rules

### Category 1: Forbidden Vendor References Outside Adapters

**Intent:** Vendor SDKs must only be used in the Adapters layer.

**Assertions:**
- No file in `src/domain/` imports from `aws-sdk`, `@aws-sdk/*`, `@octokit/*`, `jira.js`, `@grafana/*`, or any vendor SDK package
- No file in `src/application/` imports from any vendor SDK package
- No file in `src/host/` imports vendor SDKs for business purposes (wiring imports are allowed)

**Implementation:** Lint rule or custom analyzer scanning import statements against a vendor SDK deny-list.

### Category 2: Forbidden Framework References in Domain

**Intent:** Domain must be free of framework coupling.

**Assertions:**
- No file in `src/domain/` imports from framework packages (Express, Fastify, NestJS, etc.)
- No file in `src/domain/` uses framework decorators (`@Controller`, `@Injectable`, `@Get`, etc.)
- No file in `src/domain/` imports HTTP types (`Request`, `Response`, `NextFunction`)

**Implementation:** Lint rule scanning for framework package imports and decorator usage.

### Category 3: No Business Logic in Host/Transport

**Intent:** Host layer is a thin shell; business logic belongs in Domain and Application.

**Assertions:**
- Route handler functions in `src/host/routes/` contain only: input parsing, use case invocation, response mapping
- No conditional business logic (`if user.role === ...`, `if amount > limit`) in host layer
- No domain entity instantiation in host layer (beyond passing to use cases)

**Implementation:** Architecture test that analyzes AST of host layer files for complexity; custom lint rule flagging business conditionals.

### Category 4: Deterministic Core Rules

**Intent:** Domain and Application must not use nondeterministic APIs directly.

**Assertions:**
- No `Date.now()` or `new Date()` in `src/domain/` or `src/application/`
- No `Math.random()` in `src/domain/` or `src/application/`
- No `crypto.randomUUID()` in `src/domain/` (unless wrapped in port)
- No `process.env` in `src/domain/` or `src/application/`
- No `fs.*` in `src/domain/` or `src/application/`
- No `child_process.*` in `src/domain/` or `src/application/`

**Implementation:** Lint rules or grep-based checks in architecture test suite.

### Category 5: Required Result/Error Handling Patterns

**Intent:** Use cases return typed results; exceptions are not used for business flow.

**Assertions:**
- Use case handler functions return `Result<T, AppError>` (or equivalent)
- No bare `throw` statements in `src/application/use-cases/` for business outcomes
- Error types extend or conform to `AppError` interface

**Implementation:** Type system enforcement + custom lint rule checking for bare throws in use case files.

### Category 6: Structured Logging Rules

**Intent:** No unstructured logging in application code.

**Assertions:**
- No `console.log`, `console.warn`, `console.error` in `src/domain/`, `src/application/`, `src/adapters/`
- Log calls use structured logger with required fields (`operationName`, `correlationId`)
- `console.*` is only allowed in `src/host/` bootstrap code

**Implementation:** Lint rule banning `console.*` in specified directories.

## Architecture Test Categories

### Category A: Dependency Direction

**Intent:** Enforce the dependency law from [Architecture](./architecture.md).

**Assertions:**

| Test | Assertion |
|---|---|
| Domain independence | No file in `src/domain/` imports from `src/application/`, `src/adapters/`, or `src/host/` |
| Application isolation | No file in `src/application/` imports from `src/adapters/` or `src/host/` |
| Adapter boundary | No file in `src/adapters/` imports from `src/host/` |

**Implementation:** Architecture test that parses import/require statements and validates against allowed dependency matrix.

### Category B: Namespace Boundaries

**Intent:** Each layer's exports are consumed only by permitted layers.

**Assertions:**
- Adapters do not re-export vendor types as part of their public module API
- Port interfaces are defined in `src/application/ports/`, not in adapters
- Domain types are not defined outside `src/domain/`

### Category C: Adapter-Only Vendor References

**Intent:** Vendor packages appear only in adapter implementations.

**Assertions:**
- `package.json` vendor dependencies are used only by files in `src/adapters/`
- Test files may reference vendor packages for test setup/teardown

### Category D: No Concrete Infrastructure in Application or Domain

**Intent:** Application and Domain depend only on abstractions (ports), never concrete implementations.

**Assertions:**
- No file in `src/application/` imports a concrete adapter class
- No file in `src/domain/` imports anything from `src/adapters/`

## Example Architecture Test

```typescript
// tests/architecture/dependency-direction.arch.spec.ts

import { getImports } from '../helpers/import-parser'
import { globSync } from 'glob'

describe('Dependency Direction', () => {
  const domainFiles = globSync('src/domain/**/*.ts')
  const applicationFiles = globSync('src/application/**/*.ts')
  const adapterFiles = globSync('src/adapters/**/*.ts')

  domainFiles.forEach(file => {
    it(`${file} must not import from application, adapters, or host`, () => {
      const imports = getImports(file)
      imports.forEach(imp => {
        expect(imp).not.toMatch(/src\/application/)
        expect(imp).not.toMatch(/src\/adapters/)
        expect(imp).not.toMatch(/src\/host/)
      })
    })
  })

  applicationFiles.forEach(file => {
    it(`${file} must not import from adapters or host`, () => {
      const imports = getImports(file)
      imports.forEach(imp => {
        expect(imp).not.toMatch(/src\/adapters/)
        expect(imp).not.toMatch(/src\/host/)
      })
    })
  })

  adapterFiles.forEach(file => {
    it(`${file} must not import from host`, () => {
      const imports = getImports(file)
      imports.forEach(imp => {
        expect(imp).not.toMatch(/src\/host/)
      })
    })
  })
})
```

## Example Lint Rule (Determinism)

```typescript
// .eslintrc rule sketch
{
  "rules": {
    "no-restricted-syntax": [
      "error",
      {
        "selector": "CallExpression[callee.object.name='Date'][callee.property.name='now']",
        "message": "Date.now() is forbidden in domain/application. Use Clock port."
      },
      {
        "selector": "NewExpression[callee.name='Date']",
        "message": "new Date() is forbidden in domain/application. Use Clock port."
      }
    ]
  }
}
```

### Category E: Security Metadata (Constitution §14)

**Intent:** Every capability must declare authorization metadata. Missing metadata = accidental exposure.

**Assertions:**

| Test | Assertion |
|---|---|
| Handler has Descriptor field | Every `*Handler` class has a `public static readonly CapabilityDescriptor Descriptor` field |
| Descriptor is valid | `Descriptor.Validate()` returns no errors |
| Handler is registered | Every handler's OperationName is in SecurityMetadataTests.AllDescriptors |
| Privileged requires audit | `IsPrivileged = true` → `RequiresAudit = true` |
| Public has no permissions | `RequiresAuthentication = false` → `RequiredPermissions` is empty |
| Unregistered denied | AuthorizationService denies unknown operations by default |

**Implementation:** `SecurityMetadataTests.cs` in Architecture.Tests project. Reflection-based enumeration of all handler types.

## Enforcement

| Category | Tool | CI Stage |
|---|---|---|
| Forbidden vendor refs | Lint rule + architecture test | Stage 2 |
| Forbidden framework refs | Lint rule | Stage 1 |
| No business logic in host | Architecture test | Stage 2 |
| Deterministic core | Lint rule | Stage 1 |
| Result/error patterns | Type system + lint | Stage 1 |
| Structured logging | Lint rule | Stage 1 |
| Dependency direction | Architecture test | Stage 2 |
| Namespace boundaries | Architecture test | Stage 2 |
| Security metadata | Architecture test (SecurityMetadataTests) | Stage 2 |
| Startup auth validation | ServiceRegistration.ValidateCapabilityRegistry() | Runtime (startup) |

See [CI Gates](./ci-gates.md).
