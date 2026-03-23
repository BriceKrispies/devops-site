using DevOpsSite.Application.Ports;
using DevOpsSite.Host.Routes;

namespace DevOpsSite.Host.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs rich context for debugging, and returns
/// a sanitized ApiErrorResponse to the client. Never leaks stack traces or
/// internal exception messages in production-oriented API responses.
/// </summary>
public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext, ITelemetryPort telemetry)
    {
        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — not an error. Log at info level and return 499.
            var correlationId = GetCorrelationId(httpContext);
            telemetry.LogInfo(
                operationName: GetOperationName(httpContext),
                correlationId: correlationId,
                message: "Request cancelled by client");

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = 499; // nginx-style client closed request
            }
        }
        catch (Exception ex)
        {
            var correlationId = GetCorrelationId(httpContext);
            var operationName = GetOperationName(httpContext);
            var actor = GetActorId(httpContext);

            // Rich internal log — includes everything needed for debugging
            telemetry.LogError(
                operationName: operationName,
                correlationId: correlationId,
                message: $"Unhandled exception: {ex.GetType().Name}: {ex.Message}",
                errorCode: "INTERNAL_ERROR",
                fields: new Dictionary<string, object>
                {
                    ["exceptionType"] = ex.GetType().FullName ?? ex.GetType().Name,
                    ["stackTrace"] = ex.StackTrace ?? "",
                    ["route"] = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                    ["actor"] = actor ?? "anonymous",
                    ["innerException"] = ex.InnerException?.Message ?? ""
                });

            if (!httpContext.Response.HasStarted)
            {
                var response = ResultMapper.InternalError(correlationId);
                httpContext.Response.StatusCode = 500;
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(response);
            }
        }
    }

    private static string GetCorrelationId(HttpContext httpContext) =>
        httpContext.Response.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? httpContext.Items["OperationContext"] switch
        {
            Application.Context.OperationContext ctx => ctx.CorrelationId,
            _ => "unknown"
        };

    private static string GetOperationName(HttpContext httpContext) =>
        httpContext.Items["OperationContext"] switch
        {
            Application.Context.OperationContext ctx => ctx.OperationName,
            _ => $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

    private static string? GetActorId(HttpContext httpContext) =>
        httpContext.Items["OperationContext"] switch
        {
            Application.Context.OperationContext ctx => ctx.Actor?.Id,
            _ => null
        };
}
