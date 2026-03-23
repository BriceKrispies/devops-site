# Frontend Capability Resolution

## Overview

The frontend consumes resolved capabilities from the backend via `GET /api/capabilities`. The backend is the source of truth for what features are available to the current user.

## How It Works

1. **Instant paint** — Area pages render immediately from the static catalog (`src/capabilities/catalog.ts`)
2. **Backend fetch** — The real capabilities adapter fetches from `/api/capabilities`
3. **Re-render** — When backend data arrives, pages re-render with resolved statuses
4. **Graceful degradation** — If the backend is unreachable, the static catalog remains visible

## Resolved Status Model

The backend returns each capability with a `status` field:

| Status | Frontend behavior |
|---|---|
| `enabled` | Clickable card, green border, interactive |
| `disabled` | Dimmed card, red border, non-interactive, shows message |
| `hidden` | Not rendered at all |
| `read_only` | Blue border, visible but actions blocked |
| `degraded` | Yellow border, partial functionality warning |

## Capability Client API

All frontend code should access capabilities through `src/capabilities/capability-client.ts`:

```typescript
import {
  isLoaded,
  isEnabled,
  isVisible,
  isActionable,
  getStatus,
  getCapability,
  getMessage,
  getByArea,
  getAllVisible,
  toCapabilityShells,
} from "../capabilities/capability-client";
```

### Key functions

- `isEnabled(key)` — Is the capability fully available?
- `isVisible(key)` — Should it be rendered? (false for `hidden`)
- `isActionable(key)` — Can the user perform actions? (false for `read_only`, `disabled`)
- `getStatus(key)` — Get the resolved status string
- `getMessage(key)` — Get the user-facing status message
- `getByArea(area)` — All visible capabilities for a feature area
- `toCapabilityShells(area?)` — Convert to legacy `CapabilityShell[]` for existing renderers

### Fail-closed defaults

If capabilities haven't loaded yet or a capability key is unknown, all checks return the most restrictive value:
- `getStatus()` → `"disabled"`
- `isEnabled()` → `false`
- `isVisible()` → `false` (only when not loaded)
- `isActionable()` → `false`

## Adding Real Capability Checks to a Page

```typescript
import { realCapabilitiesAdapter } from "../../adapters/real/capabilities";
import { registerAdapters } from "../../adapters/registry";
import { TOPICS } from "../../state/topics";
import type { ResolvedCapability } from "../../capabilities/resolved-types";
import type { TopicPayload } from "../../state/topics";

const store = createStore();
registerAdapters(store, [realCapabilitiesAdapter]);

store.subscribe<TopicPayload<ResolvedCapability[]>>(TOPICS.RESOLVED_CAPABILITIES, (payload) => {
  if (payload.status === "ok" && payload.data) {
    // Re-render with resolved capabilities
  }
});
```

## Route Stability

Routes remain stable when capabilities change status. Pages always render a shell with skeleton/unavailable states rather than redirecting or 404-ing. This ensures bookmarked URLs continue to work.
