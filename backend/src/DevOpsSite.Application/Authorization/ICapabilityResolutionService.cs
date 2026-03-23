using DevOpsSite.Application.Context;

namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Resolves the runtime status of all capabilities for a given actor/context.
/// This is the single entry point for the GET /api/capabilities endpoint.
/// </summary>
public interface ICapabilityResolutionService
{
    /// <summary>
    /// Resolve all capabilities from the catalog for the current context.
    /// Returns a map of OperationName → ResolvedCapability.
    /// </summary>
    IReadOnlyList<ResolvedCapability> ResolveAll(OperationContext ctx);

    /// <summary>
    /// Resolve a single capability by operation name.
    /// Returns null if the capability is not in the catalog.
    /// </summary>
    ResolvedCapability? Resolve(string operationName, OperationContext ctx);

    /// <summary>
    /// Check if a capability is currently blocked by a kill switch.
    /// This is a fast check for use in enforcement paths.
    /// </summary>
    bool IsKillSwitched(string operationName);
}
