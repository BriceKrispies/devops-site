using DevOpsSite.Application.UseCases;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Transport shell for work items. Constitution §12: thin shell only.
/// Parse input → invoke use case → map response. No business logic.
/// </summary>
public static class WorkItemRoutes
{
    public static void MapWorkItemRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/workitems/{key}", async (
            string key,
            HttpContext httpContext,
            GetWorkItemHandler handler) =>
        {
            var ctx = httpContext.GetOperationContext() with
            {
                OperationName = GetWorkItemHandler.OperationName
            };

            var query = new GetWorkItemQuery { Key = key };
            var result = await handler.HandleAsync(query, ctx, httpContext.RequestAborted);

            if (result.IsSuccess)
            {
                return Results.Ok(new
                {
                    key = result.Value.Key.Value,
                    title = result.Value.Title,
                    status = result.Value.Status,
                    category = result.Value.Category,
                    assignee = result.Value.Assignee,
                    url = result.Value.Url,
                    provider = result.Value.Provider,
                    retrievedAt = result.Value.RetrievedAt
                });
            }

            return ResultMapper.ToHttpResult(result.Error);
        });
    }
}
