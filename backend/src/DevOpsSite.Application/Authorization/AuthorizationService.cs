using DevOpsSite.Application.Context;

namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Default authorization service. Evaluates actor context against capability descriptors.
/// Deny by default: missing descriptor = denied.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly CapabilityRegistry _registry;

    public AuthorizationService(CapabilityRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public AuthorizationResult Evaluate(string operationName, OperationContext ctx)
    {
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
