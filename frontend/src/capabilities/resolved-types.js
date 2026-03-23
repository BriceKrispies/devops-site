/**
 * Maps a ResolvedStatus to the legacy CapabilityStatus used by existing renderers.
 * This allows gradual migration — new code uses ResolvedStatus directly,
 * existing renderers can continue using the legacy type.
 */
export function resolvedStatusToLegacy(status) {
    switch (status) {
        case "enabled":
            return "ready";
        case "degraded":
            return "stub";
        case "disabled":
            return "disabled";
        case "hidden":
            return "disabled";
        case "read_only":
            return "ready";
        default:
            return "disabled";
    }
}
