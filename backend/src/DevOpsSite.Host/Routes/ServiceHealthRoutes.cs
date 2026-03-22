using DevOpsSite.Application.UseCases;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Transport shell for service health. Constitution §12: thin shell only.
/// Parse input → invoke use case → map response. No business logic.
/// </summary>
public static class ServiceHealthRoutes
{
    public static void MapServiceHealthRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/health/{serviceId}", async (
            string serviceId,
            HttpContext httpContext,
            GetServiceHealthHandler handler) =>
        {
            var ctx = httpContext.GetOperationContext() with
            {
                OperationName = GetServiceHealthHandler.OperationName
            };

            var query = new GetServiceHealthQuery { ServiceId = serviceId };
            var result = await handler.HandleAsync(query, ctx, httpContext.RequestAborted);

            if (result.IsSuccess)
            {
                return Results.Ok(new
                {
                    serviceId = result.Value.ServiceId.Value,
                    status = result.Value.Status.ToString(),
                    description = result.Value.Description,
                    checkedAt = result.Value.CheckedAt
                });
            }

            return ResultMapper.ToHttpResult(result.Error);
        });
    }
}
