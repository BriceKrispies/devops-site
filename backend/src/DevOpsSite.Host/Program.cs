using DevOpsSite.Adapters.Configuration;
using DevOpsSite.Host.Authentication;
using DevOpsSite.Host.Composition;
using DevOpsSite.Host.Middleware;
using DevOpsSite.Host.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBackendServices(builder.Configuration);

var app = builder.Build();

// Startup validation — Constitution §14: fail-fast on missing security metadata
// Also validates the DevelopmentBypass guard (§13.5)
app.Services.ValidateCapabilityRegistry();

// Global exception handler — outermost middleware, catches all unhandled exceptions
// and returns sanitized ApiErrorResponse. Must be registered before all other middleware.
app.UseMiddleware<ExceptionHandlerMiddleware>();

// CORS for local development (frontend on different port)
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseMiddleware<OperationContextMiddleware>();

// Auth middleware: resolves identity + permissions.
// DevelopmentBypass → local persona injection.
// Oidc → JWT bearer validation + DynamoDB user lookup.
var authConfig = app.Services.GetRequiredService<AuthConfig>();
if (AuthMode.IsDevelopmentBypass(authConfig.Mode))
{
    app.UseMiddleware<DevelopmentAuthMiddleware>();
}
else if (string.Equals(authConfig.Mode, AuthMode.Oidc, StringComparison.OrdinalIgnoreCase))
{
    app.UseAuthentication();
    app.UseMiddleware<OidcAuthMiddleware>();
}

app.MapAuthRoutes();
app.MapServiceHealthRoutes();
app.MapWorkItemRoutes();
app.MapTraceRoutes();
app.MapCapabilitiesRoutes();

app.Run();
