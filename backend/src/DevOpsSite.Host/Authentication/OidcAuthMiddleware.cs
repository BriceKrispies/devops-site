using System.Security.Claims;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Authentication;

/// <summary>
/// OIDC middleware that resolves user identity and permissions from DynamoDB
/// after ASP.NET's JWT bearer handler has validated the token.
///
/// Runs AFTER OperationContextMiddleware (which creates the base context)
/// and AFTER UseAuthentication() (which validates the JWT).
///
/// If the user is authenticated (valid JWT) but not found in DynamoDB,
/// the request proceeds as anonymous — deny-by-default handles the rest.
/// </summary>
public sealed class OidcAuthMiddleware
{
    private readonly RequestDelegate _next;

    public OidcAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, IUserResolutionPort userResolution, ITelemetryPort telemetry)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var email = ExtractEmail(httpContext.User);

            if (!string.IsNullOrEmpty(email))
            {
                var resolved = await userResolution.ResolveByEmailAsync(email, httpContext.RequestAborted);

                if (resolved is not null)
                {
                    var baseCtx = httpContext.GetOperationContext();

                    var authenticatedCtx = baseCtx with
                    {
                        Actor = new ActorIdentity
                        {
                            Id = resolved.UserId,
                            Type = ActorType.User,
                            DisplayName = resolved.Username
                        },
                        Permissions = resolved.Permissions
                    };

                    httpContext.Items["OperationContext"] = authenticatedCtx;
                }
                else
                {
                    telemetry.LogWarn("OidcAuth", "n/a",
                        $"Authenticated user '{email}' not found in DynamoDB user store.");
                }
            }
        }

        await _next(httpContext);
    }

    private static string? ExtractEmail(ClaimsPrincipal principal)
    {
        // Cognito puts email in the "email" claim.
        // Also check standard claim types as fallback.
        return principal.FindFirstValue("email")
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("cognito:username");
    }
}
