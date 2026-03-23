# Capability Resolution System

## Overview

The capability resolution system makes the backend the **source of truth** for what features and actions are available to a given user. The frontend consumes a single resolved capability payload from `GET /api/capabilities` and renders accordingly.

## Resolution Order

When resolving a capability's runtime status, the system applies these rules in strict priority order:

1. **Kill switch** — Hard override. If active, capability is `Disabled` regardless of everything else.
2. **Implementation status** — `Planned`/`Stub` capabilities resolve to `Disabled`/`Degraded`.
3. **Auth/role check** — Missing authentication → `Hidden`. Missing permissions → `Hidden` or `ReadOnly`.
4. **Explicit runtime override** — Admin-set status override (e.g., maintenance mode).
5. **Default** — If all checks pass, capability is `Enabled`.

## Resolved Status Values

| Status | Meaning | Frontend behavior |
|---|---|---|
| `Enabled` | Fully available | Show as interactive, navigable |
| `Disabled` | Blocked (kill switch, not implemented, or explicit) | Show card, dimmed, non-interactive |
| `Hidden` | Should not be rendered | Omit from UI entirely |
| `ReadOnly` | Visible but actions blocked | Show card, read-only indicator |
| `Degraded` | Partially available | Show card with warning |

## API Contract

### `GET /api/capabilities`

Returns all capabilities resolved for the current authenticated user.

```json
{
  "capabilities": [
    {
      "key": "GetServiceHealth",
      "status": "enabled",
      "name": "Retrieve normalized health status for a service",
      "area": "overview",
      "description": "Retrieve normalized health status for a service.",
      "risk": "low",
      "route": "/",
      "message": null,
      "reason": null,
      "permissions": ["servicehealth:read"],
      "metadata": {
        "category": "ServiceHealth",
        "privileged": false,
        "executionMode": "synchronous"
      }
    }
  ]
}
```

## Kill Switches

Kill switches provide an emergency stop for any capability. They are the highest-priority resolution rule.

### How it works

- Kill switches are stored in `ICapabilityOverrideStore` (in-memory by default)
- When active, a kill switch blocks both:
  - The capability's **resolved status** (returns `Disabled` to frontend)
  - The capability's **runtime execution** (AuthorizationService returns `KillSwitchActive`)
- Kill switches include a reason, activator, and timestamp for audit

### Adding a kill switch at runtime

```csharp
var store = app.Services.GetRequiredService<ICapabilityOverrideStore>();
store.SetKillSwitch(new KillSwitch
{
    OperationName = "QueuesRedriveDlq",
    IsActive = true,
    Reason = "Investigating data corruption issue",
    ActivatedBy = "ops-team",
    ActivatedAt = DateTimeOffset.UtcNow
});
```

### Deactivating a kill switch

```csharp
store.SetKillSwitch(new KillSwitch
{
    OperationName = "QueuesRedriveDlq",
    IsActive = false
});
```

## Runtime Overrides

Overrides let an admin force a specific resolved status for a capability, applied after kill switches and implementation checks but before the default.

```csharp
store.SetOverride(new CapabilityOverride
{
    OperationName = "GetServiceHealth",
    Status = ResolvedCapabilityStatus.ReadOnly,
    Reason = "Maintenance window",
    SetBy = "admin",
    SetAt = DateTimeOffset.UtcNow
});
```

## Adding a New Capability

1. Add a `CapabilityDescriptor` entry in `OperationalCapabilityCatalog.cs`
2. Set `Status = ImplementationStatus.Planned` initially
3. When implementing, create a handler with `Status = ImplementationStatus.Ready`
4. Register the descriptor in `ServiceRegistration.AddBackendServices()`
5. The capability automatically appears in `GET /api/capabilities`

## Backend Enforcement

Every use case handler already evaluates authorization via `_authz.Evaluate()`. The `AuthorizationService` now also checks kill switches. If a kill switch is active:

- `AuthorizationResult.IsAllowed` is `false`
- `AuthorizationResult.FailureReason` is `KillSwitchActive`
- The handler maps this to a 503 (Service Unavailable) response

No handler code changes are needed — kill switch enforcement is automatic.

## Frontend Consumption

The frontend fetches `GET /api/capabilities` at page load and provides centralized access via `src/capabilities/capability-client.ts`:

```typescript
import { isEnabled, isVisible, getStatus, getByArea } from "./capabilities/capability-client";

// Check if a capability is available
if (isEnabled("GetServiceHealth")) { /* render interactive UI */ }

// Check visibility (hidden capabilities should not be shown)
if (isVisible("QueuesRead")) { /* render card */ }

// Get all capabilities for an area
const queueCaps = getByArea("queues");
```

The frontend **fails closed**: if capabilities haven't loaded or a capability is unknown, it defaults to `disabled`.

## Key Files

### Backend
- `Application/Authorization/CapabilityResolutionService.cs` — Resolution logic
- `Application/Authorization/ICapabilityResolutionService.cs` — Interface
- `Application/Authorization/ResolvedCapability.cs` — Resolved capability model
- `Application/Authorization/ResolvedCapabilityStatus.cs` — Status enum
- `Application/Authorization/KillSwitch.cs` — Kill switch model
- `Application/Authorization/CapabilityOverride.cs` — Override model
- `Application/Ports/ICapabilityOverrideStore.cs` — Override store port
- `Adapters/Capabilities/InMemoryCapabilityOverrideStore.cs` — In-memory store
- `Host/Routes/CapabilitiesRoutes.cs` — API endpoint

### Frontend
- `capabilities/resolved-types.ts` — TypeScript types for backend contract
- `capabilities/capability-client.ts` — Central accessor layer
- `adapters/real/capabilities.ts` — Real adapter fetching from backend
- `ui/renderers/resolved-capability-card.ts` — Resolved capability card renderer
