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

// CORS for local development (frontend on different port)
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseMiddleware<OperationContextMiddleware>();

// Dev auth middleware: injects local persona identity.
// Only active when Auth:Mode is "DevelopmentBypass".
// The startup guard above already verified this is only in Development.
var authConfig = app.Services.GetRequiredService<AuthConfig>();
if (AuthMode.IsDevelopmentBypass(authConfig.Mode))
{
    app.UseMiddleware<DevelopmentAuthMiddleware>();
}

app.MapServiceHealthRoutes();
app.MapWorkItemRoutes();
app.MapTraceRoutes();

app.Run();
