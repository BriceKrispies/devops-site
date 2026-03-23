using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Default authorization service. Evaluates actor context against capability descriptors.
/// Deny by default: missing descriptor = denied.
/// Kill switches are checked first and override all other rules.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly CapabilityRegistry _registry;
    private readonly ICapabilityOverrideStore? _overrideStore;

    public AuthorizationService(CapabilityRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public AuthorizationService(CapabilityRegistry registry, ICapabilityOverrideStore overrideStore)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _overrideStore = overrideStore ?? throw new ArgumentNullException(nameof(overrideStore));
    }

    public AuthorizationResult Evaluate(string operationName, OperationContext ctx)
    {
        // Kill switch — highest priority, blocks everything
        if (_overrideStore is not null)
        {
            var killSwitch = _overrideStore.GetKillSwitch(operationName);
            if (killSwitch is { IsActive: true })
                return AuthorizationResult.KillSwitched(operationName, killSwitch.Reason);
        }

        var descriptor = _registry.GetDescriptor(operationName);

        // Deny by default: no descriptor = no access
        if (descriptor is null)
            return AuthorizationResult.MissingDescriptor(operationName);

        // Public capability: no auth required
        if (!descriptor.RequiresAuthentication)
            return AuthorizationResult.Allowed();

        // Auth required: actor must be present
        if (ctx.Actor is null)
            return AuthorizationResult.Unauthenticated();

        // Check permissions
        if (descriptor.RequiredPermissions.Count > 0)
        {
            var actorPermissions = ctx.Permissions;
            foreach (var required in descriptor.RequiredPermissions)
            {
                if (!actorPermissions.Contains(required))
                    return AuthorizationResult.Forbidden(
                        $"Missing required permission: {required.Value}");
            }
        }

        return AuthorizationResult.Allowed();
    }
}
