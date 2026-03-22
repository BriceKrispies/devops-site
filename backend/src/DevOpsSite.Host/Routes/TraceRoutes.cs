using DevOpsSite.Application.UseCases;
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Transport shell for trace events. Constitution §12: thin shell only.
/// Parse input → invoke use case → map response. No business logic.
/// </summary>
public static class TraceRoutes
{
    public static void MapTraceRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/traces", async (
            TraceEventsRequest request,
            HttpContext httpContext,
            AddTraceEventsHandler handler) =>
        {
            var ctx = httpContext.GetOperationContext() with
            {
                OperationName = AddTraceEventsHandler.OperationName
            };

            var command = new AddTraceEventsCommand
            {
                Events = (request.Events ?? []).Select(e => new TraceEventInput
                {
                    Id = e.Id,
                    SourceSystem = e.SourceSystem,
                    EventType = e.EventType,
                    OccurredAt = e.OccurredAt,
                    DisplayTitle = e.DisplayTitle,
                    Tags = e.Tags,
                    ServiceName = e.ServiceName,
                    RelatedIdentifiers = e.RelatedIdentifiers,
                    SourceUrl = e.SourceUrl,
                    Provenance = e.Provenance
                }).ToList()
            };

            var result = await handler.HandleAsync(command, ctx, httpContext.RequestAborted);

            if (result.IsSuccess)
                return Results.Ok(new { appended = result.Value });

            return ResultMapper.ToHttpResult(result.Error);
        });

        routes.MapGet("/api/traces", async (
            string? serviceName,
            string? eventType,
            string? sourceSystem,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? limit,
            HttpContext httpContext,
            QueryTraceEventsHandler handler) =>
        {
            var ctx = httpContext.GetOperationContext() with
            {
                OperationName = QueryTraceEventsHandler.OperationName
            };

            var query = new QueryTraceEventsQuery
            {
                ServiceName = serviceName,
                EventType = eventType,
                SourceSystem = sourceSystem,
                From = from,
                To = to,
                Limit = limit ?? 100
            };

            var result = await handler.HandleAsync(query, ctx, httpContext.RequestAborted);

            if (result.IsSuccess)
            {
                return Results.Ok(new
                {
                    events = result.Value.Select(e => new
                    {
                        id = e.Id.Value,
                        sourceSystem = e.SourceSystem,
                        eventType = e.EventType.Value,
                        occurredAt = e.OccurredAt,
                        displayTitle = e.DisplayTitle,
                        tags = e.Tags,
                        serviceName = e.ServiceName,
                        relatedIdentifiers = e.RelatedIdentifiers,
                        sourceUrl = e.SourceUrl,
                        provenance = e.Provenance
                    }),
                    count = result.Value.Count
                });
            }

            return ResultMapper.ToHttpResult(result.Error);
        });
    }
}

/// <summary>Request DTO for POST /api/traces. Transport-only type.</summary>
public sealed record TraceEventsRequest
{
    public List<TraceEventRequestItem>? Events { get; init; }
}

public sealed record TraceEventRequestItem
{
    public string Id { get; init; } = "";
    public string SourceSystem { get; init; } = "";
    public string EventType { get; init; } = "";
    public DateTimeOffset OccurredAt { get; init; }
    public string DisplayTitle { get; init; } = "";
    public List<string>? Tags { get; init; }
    public string? ServiceName { get; init; }
    public Dictionary<string, string>? RelatedIdentifiers { get; init; }
    public string? SourceUrl { get; init; }
    public string? Provenance { get; init; }
}
