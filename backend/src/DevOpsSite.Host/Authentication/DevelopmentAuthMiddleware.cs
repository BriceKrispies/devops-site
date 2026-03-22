using DevOpsSite.Adapters.Configuration;
using DevOpsSite.Application.Context;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Authentication;

/// <summary>
/// Development-only middleware that injects a local persona into the OperationContext.
/// Produces the same normalized ActorIdentity + Permissions the rest of the system expects.
///
/// This middleware runs AFTER OperationContextMiddleware (which creates the base context)
/// and replaces the anonymous context with an authenticated one based on the configured persona.
///
/// SAFETY: Only registered when Auth:Mode is "DevelopmentBypass".
/// The startup guard in ServiceRegistration prevents this from being active outside Development.
/// </summary>
public sealed class DevelopmentAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DevPersona _persona;

    public DevelopmentAuthMiddleware(RequestDelegate next, AuthConfig authConfig)
    {
        _next = next;
        _persona = DevPersonas.GetPersona(authConfig.ActivePersona);
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Get the context created by OperationContextMiddleware
        var baseCtx = httpContext.GetOperationContext();

        // Replace with authenticated context using the dev persona
        var authenticatedCtx = baseCtx with
        {
            Actor = _persona.ToActor(),
            Permissions = _persona.Permissions
        };

        httpContext.Items["OperationContext"] = authenticatedCtx;

        // Add response header so developers can see which persona is active
        httpContext.Response.Headers["X-Dev-Persona"] = _persona.Id;

        await _next(httpContext);
    }
}
