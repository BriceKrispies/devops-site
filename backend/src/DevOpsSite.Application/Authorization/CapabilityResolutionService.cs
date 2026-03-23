using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Resolves capability status using the following precedence:
///   1. Kill switch (hard override — blocks everything)
///   2. Implementation status (Planned/Stub → not available)
///   3. Auth/role check (user lacks required permissions)
///   4. Explicit runtime override (admin set a specific status)
///   5. Default (enabled if Ready and authorized)
/// </summary>
public sealed class CapabilityResolutionService : ICapabilityResolutionService
{
    private readonly ICapabilityOverrideStore _overrideStore;

    // Category → frontend area mapping
    private static readonly Dictionary<CapabilityCategory, string> CategoryToArea = new()
    {
        [CapabilityCategory.Traces] = "investigate",
        [CapabilityCategory.ServiceHealth] = "overview",
        [CapabilityCategory.WorkItems] = "overview",
        [CapabilityCategory.Queues] = "queues",
        [CapabilityCategory.Databases] = "databases",
        [CapabilityCategory.Logs] = "logs",
        [CapabilityCategory.Admin] = "admin"
    };

    // Category → frontend route mapping for planned capabilities
    private static readonly Dictionary<string, string?> CapabilityRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dashboard"] = "/",
        ["QueryTraceEvents"] = "/src/pages/trace.html",
        ["GetServiceHealth"] = "/",
        ["GetWorkItem"] = "/",
        ["AddTraceEvents"] = null,
        ["IngestTraceEvents"] = null,
        ["QueuesRead"] = "/src/pages/queues.html",
        ["QueuesRedriveDlq"] = "/src/pages/queues.html",
        ["DatabasesRead"] = "/src/pages/databases.html",
        ["DatabasesCloneNonProd"] = "/src/pages/databases.html",
        ["LogsRead"] = "/src/pages/logs.html"
    };

    public CapabilityResolutionService(ICapabilityOverrideStore overrideStore)
    {
        _overrideStore = overrideStore ?? throw new ArgumentNullException(nameof(overrideStore));
    }

    public IReadOnlyList<ResolvedCapability> ResolveAll(OperationContext ctx)
    {
        return OperationalCapabilityCatalog.All
            .Select(descriptor => ResolveDescriptor(descriptor, ctx))
            .ToList();
    }

    public ResolvedCapability? Resolve(string operationName, OperationContext ctx)
    {
        var descriptor = OperationalCapabilityCatalog.GetByOperationName(operationName);
        if (descriptor is null) return null;

        return ResolveDescriptor(descriptor, ctx);
    }

    public bool IsKillSwitched(string operationName)
    {
        var ks = _overrideStore.GetKillSwitch(operationName);
        return ks is { IsActive: true };
    }

    private ResolvedCapability ResolveDescriptor(CapabilityDescriptor descriptor, OperationContext ctx)
    {
        var area = CategoryToArea.GetValueOrDefault(descriptor.Category, "operations");
        CapabilityRoutes.TryGetValue(descriptor.OperationName, out var route);

        var baseCapability = new ResolvedCapability
        {
            Key = descriptor.OperationName,
            Status = ResolvedCapabilityStatus.Enabled,
            Name = descriptor.Description?.Split('.').FirstOrDefault()?.Trim() ?? descriptor.OperationName,
            Area = area,
            Description = descriptor.Description ?? descriptor.OperationName,
            Risk = descriptor.RiskLevel.ToString().ToLowerInvariant(),
            Route = route,
            Permissions = descriptor.RequiredPermissions.Select(p => p.Value).ToList(),
            Metadata = new ResolvedCapabilityMetadata
            {
                Category = descriptor.Category.ToString(),
                Privileged = descriptor.IsPrivileged,
                ExecutionMode = descriptor.ExecutionMode.ToString().ToLowerInvariant()
            }
        };

        // 1. Kill switch — highest priority
        var killSwitch = _overrideStore.GetKillSwitch(descriptor.OperationName);
        if (killSwitch is { IsActive: true })
        {
            return baseCapability with
            {
                Status = ResolvedCapabilityStatus.Disabled,
                Message = killSwitch.Reason ?? "This capability has been emergency-disabled.",
                Reason = "kill_switch"
            };
        }

        // 2. Implementation status
        if (descriptor.Status == ImplementationStatus.Planned)
        {
            return baseCapability with
            {
                Status = ResolvedCapabilityStatus.Disabled,
                Message = "This capability is not yet implemented.",
                Reason = "not_implemented",
                Route = null
            };
        }

        if (descriptor.Status == ImplementationStatus.Stub)
        {
            return baseCapability with
            {
                Status = ResolvedCapabilityStatus.Degraded,
                Message = "This capability is under development.",
                Reason = "stub"
            };
        }

        if (descriptor.Status == ImplementationStatus.Disabled)
        {
            return baseCapability with
            {
                Status = ResolvedCapabilityStatus.Disabled,
                Message = "This capability has been disabled.",
                Reason = "disabled_by_code"
            };
        }

        // 3. Auth/role check
        if (descriptor.RequiresAuthentication)
        {
            if (ctx.Actor is null)
            {
                return baseCapability with
                {
                    Status = ResolvedCapabilityStatus.Hidden,
                    Message = "Authentication required.",
                    Reason = "unauthenticated"
                };
            }

            if (descriptor.RequiredPermissions.Count > 0)
            {
                var actorPermissions = ctx.Permissions;
                var hasAllPermissions = descriptor.RequiredPermissions
                    .All(required => actorPermissions.Contains(required));

                if (!hasAllPermissions)
                {
                    // For read operations, hide entirely. For write operations that have
                    // a corresponding read permission the user has, show as read_only.
                    var hasAnyPermission = descriptor.RequiredPermissions
                        .Any(required => actorPermissions.Contains(required));

                    if (hasAnyPermission)
                    {
                        return baseCapability with
                        {
                            Status = ResolvedCapabilityStatus.ReadOnly,
                            Message = "You do not have full permissions for this capability.",
                            Reason = "forbidden"
                        };
                    }

                    return baseCapability with
                    {
                        Status = ResolvedCapabilityStatus.Hidden,
                        Message = "Insufficient permissions.",
                        Reason = "forbidden"
                    };
                }
            }
        }

        // 4. Explicit runtime override
        var capOverride = _overrideStore.GetOverride(descriptor.OperationName);
        if (capOverride is not null)
        {
            return baseCapability with
            {
                Status = capOverride.Status,
                Message = capOverride.Reason,
                Reason = "override"
            };
        }

        // 5. Default — enabled
        return baseCapability;
    }
}
