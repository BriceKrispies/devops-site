using DevOpsSite.Adapters.Configuration;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Auth-related routes. Constitution §12: thin transport shell only.
/// </summary>
public static class AuthRoutes
{
    public static void MapAuthRoutes(this IEndpointRouteBuilder routes)
    {
        // Returns current user info, or 401 if not authenticated
        routes.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            var ctx = httpContext.GetOperationContext();

            if (!ctx.IsAuthenticated || ctx.Actor is null)
            {
                return Results.Json(
                    new { error = "Not authenticated" },
                    statusCode: 401);
            }

            return Results.Ok(new
            {
                id = ctx.Actor.Id,
                displayName = ctx.Actor.DisplayName,
                permissions = ctx.Permissions.Select(p => p.Value).OrderBy(p => p)
            });
        });

        // Returns OIDC config for frontend, or 404 if DevelopmentBypass
        routes.MapGet("/api/auth/config", (AuthConfig authConfig) =>
        {
            if (AuthMode.IsDevelopmentBypass(authConfig.Mode))
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                authority = authConfig.Authority,
                clientId = authConfig.ClientId,
                scope = "email openid phone"
            });
        });
    }
}
