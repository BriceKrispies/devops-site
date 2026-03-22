using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Queries;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;

namespace DevOpsSite.Application.UseCases;

public sealed record QueryTraceEventsQuery
{
    public string? ServiceName { get; init; }
    public string? EventType { get; init; }
    public string? SourceSystem { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed class QueryTraceEventsHandler
{
    private readonly ITraceStorePort _traceStore;
    private readonly ITelemetryPort _telemetry;
    private readonly IAuthorizationService _authz;

    public const string OperationName = "QueryTraceEvents";

    public static readonly CapabilityDescriptor Descriptor = new()
    {
        OperationName = OperationName,
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.TraceEventsRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Query normalized trace events from the store.",
        Category = CapabilityCategory.Traces,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    public QueryTraceEventsHandler(ITraceStorePort traceStore, ITelemetryPort telemetry, IAuthorizationService authz)
    {
        _traceStore = traceStore ?? throw new ArgumentNullException(nameof(traceStore));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _authz = authz ?? throw new ArgumentNullException(nameof(authz));
    }

    public async Task<Result<IReadOnlyList<TraceEvent>>> HandleAsync(QueryTraceEventsQuery query, OperationContext ctx, CancellationToken ct = default)
    {
        var contextErrors = ctx.Validate();
        if (contextErrors.Count > 0)
            return Result<IReadOnlyList<TraceEvent>>.Failure(
                AppError.InvariantViolation(
                    $"Invalid operation context: {string.Join("; ", contextErrors)}",
                    OperationName, ctx.CorrelationId ?? "unknown"));

        var authResult = _authz.Evaluate(OperationName, ctx);
        if (!authResult.IsAllowed)
        {
            return authResult.FailureReason == AuthorizationFailureReason.Unauthenticated
                ? Result<IReadOnlyList<TraceEvent>>.Failure(
                    AppError.Unauthenticated(authResult.Message!, OperationName, ctx.CorrelationId))
                : Result<IReadOnlyList<TraceEvent>>.Failure(
                    AppError.Forbidden(authResult.Message!, OperationName, ctx.CorrelationId));
        }

        var traceQuery = new TraceQuery
        {
            ServiceName = query.ServiceName,
            EventType = query.EventType,
            SourceSystem = query.SourceSystem,
            From = query.From,
            To = query.To,
            Limit = query.Limit
        };

        var queryErrors = traceQuery.Validate();
        if (queryErrors.Count > 0)
            return Result<IReadOnlyList<TraceEvent>>.Failure(
                AppError.Validation(
                    $"Invalid query: {string.Join("; ", queryErrors)}",
                    OperationName, ctx.CorrelationId));

        using var span = _telemetry.StartSpan(OperationName, ctx.CorrelationId);

        var result = await _traceStore.QueryAsync(traceQuery, ctx, ct);

        if (result.IsSuccess)
        {
            span.SetResult("success");
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "success"
            });
            _telemetry.LogInfo(OperationName, ctx.CorrelationId,
                $"Query returned {result.Value.Count} trace events.",
                new Dictionary<string, object>
                {
                    ["resultCount"] = result.Value.Count,
                    ["serviceName"] = query.ServiceName ?? "(all)",
                    ["eventType"] = query.EventType ?? "(all)"
                });
        }
        else
        {
            span.SetError(result.Error.Code.ToString(), result.Error.Message);
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "failure"
            });
            _telemetry.LogError(OperationName, ctx.CorrelationId,
                result.Error.Message, result.Error.Code.ToString(), result.Error.Dependency);
        }

        return result;
    }
}
