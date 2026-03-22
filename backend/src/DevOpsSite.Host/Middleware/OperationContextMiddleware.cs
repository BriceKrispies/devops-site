using DevOpsSite.Application.Context;

namespace DevOpsSite.Host.Middleware;

/// <summary>
/// Middleware that creates and validates OperationContext for every request.
/// Constitution §7.1: Every operation must carry context. Missing context is a failure.
/// </summary>
public sealed class OperationContextMiddleware
{
    private readonly RequestDelegate _next;

    public OperationContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        var ctx = new OperationContext
        {
            CorrelationId = correlationId,
            OperationName = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest
        };

        httpContext.Items["OperationContext"] = ctx;
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        await _next(httpContext);
    }
}

public static class OperationContextExtensions
{
    public static OperationContext GetOperationContext(this HttpContext httpContext) =>
        httpContext.Items["OperationContext"] as OperationContext
            ?? throw new InvalidOperationException("OperationContext not found. Is OperationContextMiddleware registered?");
}
