using DevOpsSite.Application.Authorization;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Transport shell for capabilities resolution. Constitution §12: thin shell only.
/// Returns the resolved capability map for the current authenticated user/session.
/// </summary>
public static class CapabilitiesRoutes
{
    public static void MapCapabilitiesRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/capabilities", (
            HttpContext httpContext,
            ICapabilityResolutionService resolutionService) =>
        {
            var ctx = httpContext.GetOperationContext();

            var capabilities = resolutionService.ResolveAll(ctx);

            return Results.Ok(new
            {
                capabilities = capabilities.Select(c => new
                {
                    key = c.Key,
                    status = c.Status.ToString().ToLowerInvariant(),
                    name = c.Name,
                    area = c.Area,
                    description = c.Description,
                    risk = c.Risk,
                    route = c.Route,
                    message = c.Message,
                    reason = c.Reason,
                    permissions = c.Permissions,
                    metadata = c.Metadata is not null ? new
                    {
                        category = c.Metadata.Category,
                        privileged = c.Metadata.Privileged,
                        executionMode = c.Metadata.ExecutionMode
                    } : null
                })
            });
        });
    }
}
