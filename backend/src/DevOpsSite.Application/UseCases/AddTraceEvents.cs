using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.UseCases;

public sealed record AddTraceEventsCommand
{
    public required IReadOnlyList<TraceEventInput> Events { get; init; }
}

public sealed record TraceEventInput
{
    public required string Id { get; init; }
    public required string SourceSystem { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string DisplayTitle { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? ServiceName { get; init; }
    public IReadOnlyDictionary<string, string>? RelatedIdentifiers { get; init; }
    public string? SourceUrl { get; init; }
    public string? Provenance { get; init; }
}

public sealed class AddTraceEventsHandler
{
    private readonly ITraceStorePort _traceStore;
    private readonly ITelemetryPort _telemetry;
    private readonly IAuthorizationService _authz;

    public const string OperationName = "AddTraceEvents";

    public static readonly CapabilityDescriptor Descriptor = new()
    {
        OperationName = OperationName,
        Category = CapabilityCategory.Traces,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default,
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.TraceEventsWrite],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Append normalized trace events to the store."
    };

    public AddTraceEventsHandler(ITraceStorePort traceStore, ITelemetryPort telemetry, IAuthorizationService authz)
    {
        _traceStore = traceStore ?? throw new ArgumentNullException(nameof(traceStore));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _authz = authz ?? throw new ArgumentNullException(nameof(authz));
    }

    public async Task<Result<int>> HandleAsync(AddTraceEventsCommand command, OperationContext ctx, CancellationToken ct = default)
    {
        var contextErrors = ctx.Validate();
        if (contextErrors.Count > 0)
            return Result<int>.Failure(
                AppError.InvariantViolation(
                    $"Invalid operation context: {string.Join("; ", contextErrors)}",
                    OperationName, ctx.CorrelationId ?? "unknown"));

        var authResult = _authz.Evaluate(OperationName, ctx);
        if (!authResult.IsAllowed)
        {
            return authResult.FailureReason == AuthorizationFailureReason.Unauthenticated
                ? Result<int>.Failure(AppError.Unauthenticated(authResult.Message!, OperationName, ctx.CorrelationId))
                : Result<int>.Failure(AppError.Forbidden(authResult.Message!, OperationName, ctx.CorrelationId));
        }

        if (command.Events is null || command.Events.Count == 0)
            return Result<int>.Failure(
                AppError.Validation("At least one trace event is required.", OperationName, ctx.CorrelationId));

        if (command.Events.Count > 500)
            return Result<int>.Failure(
                AppError.Validation("Cannot append more than 500 events in a single batch.", OperationName, ctx.CorrelationId));

        // Convert inputs to domain entities
        var domainEvents = new List<TraceEvent>(command.Events.Count);
        foreach (var input in command.Events)
        {
            try
            {
                var traceEvent = TraceEvent.Create(
                    TraceEventId.Create(input.Id),
                    input.SourceSystem,
                    TraceEventType.Create(input.EventType),
                    input.OccurredAt,
                    input.DisplayTitle,
                    input.Tags,
                    input.ServiceName,
                    input.RelatedIdentifiers,
                    input.SourceUrl,
                    input.Provenance);
                domainEvents.Add(traceEvent);
            }
            catch (ArgumentException ex)
            {
                return Result<int>.Failure(
                    AppError.Validation($"Invalid trace event '{input.Id}': {ex.Message}", OperationName, ctx.CorrelationId));
            }
        }

        using var span = _telemetry.StartSpan(OperationName, ctx.CorrelationId);

        var result = await _traceStore.AppendAsync(domainEvents, ctx, ct);

        if (result.IsSuccess)
        {
            span.SetResult("success");
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "success"
            });
            _telemetry.LogInfo(OperationName, ctx.CorrelationId,
                $"Appended {result.Value} trace events.",
                new Dictionary<string, object>
                {
                    ["eventCount"] = result.Value
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
