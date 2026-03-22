using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.UseCases;

public sealed record IngestTraceEventsCommand
{
    public int MaxBatchSize { get; init; } = 100;
}

public sealed record IngestTraceEventsResult
{
    public int Fetched { get; init; }
    public int Stored { get; init; }
}

public sealed class IngestTraceEventsHandler
{
    private readonly ITraceIngestionSourcePort _source;
    private readonly ITraceStorePort _traceStore;
    private readonly ITelemetryPort _telemetry;
    private readonly IAuthorizationService _authz;

    public const string OperationName = "IngestTraceEvents";

    public static readonly CapabilityDescriptor Descriptor = new()
    {
        OperationName = OperationName,
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.TraceEventsIngest],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Fetch pending trace events from an ingestion source and store them.",
        Category = CapabilityCategory.Traces,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    public IngestTraceEventsHandler(
        ITraceIngestionSourcePort source,
        ITraceStorePort traceStore,
        ITelemetryPort telemetry,
        IAuthorizationService authz)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _traceStore = traceStore ?? throw new ArgumentNullException(nameof(traceStore));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _authz = authz ?? throw new ArgumentNullException(nameof(authz));
    }

    public async Task<Result<IngestTraceEventsResult>> HandleAsync(
        IngestTraceEventsCommand command, OperationContext ctx, CancellationToken ct = default)
    {
        var contextErrors = ctx.Validate();
        if (contextErrors.Count > 0)
            return Result<IngestTraceEventsResult>.Failure(
                AppError.InvariantViolation(
                    $"Invalid operation context: {string.Join("; ", contextErrors)}",
                    OperationName, ctx.CorrelationId ?? "unknown"));

        var authResult = _authz.Evaluate(OperationName, ctx);
        if (!authResult.IsAllowed)
        {
            return authResult.FailureReason == AuthorizationFailureReason.Unauthenticated
                ? Result<IngestTraceEventsResult>.Failure(
                    AppError.Unauthenticated(authResult.Message!, OperationName, ctx.CorrelationId))
                : Result<IngestTraceEventsResult>.Failure(
                    AppError.Forbidden(authResult.Message!, OperationName, ctx.CorrelationId));
        }

        if (command.MaxBatchSize < 1 || command.MaxBatchSize > 500)
            return Result<IngestTraceEventsResult>.Failure(
                AppError.Validation("MaxBatchSize must be between 1 and 500.", OperationName, ctx.CorrelationId));

        using var span = _telemetry.StartSpan(OperationName, ctx.CorrelationId);

        // Fetch pending events from the source
        var fetchResult = await _source.FetchPendingAsync(command.MaxBatchSize, ctx, ct);
        if (fetchResult.IsFailure)
        {
            span.SetError(fetchResult.Error.Code.ToString(), fetchResult.Error.Message);
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "failure"
            });
            _telemetry.LogError(OperationName, ctx.CorrelationId,
                fetchResult.Error.Message, fetchResult.Error.Code.ToString(), fetchResult.Error.Dependency);
            return Result<IngestTraceEventsResult>.Failure(fetchResult.Error);
        }

        var pending = fetchResult.Value;
        if (pending.Count == 0)
        {
            span.SetResult("success");
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "success"
            });
            _telemetry.LogInfo(OperationName, ctx.CorrelationId,
                "No pending trace events to ingest.",
                new Dictionary<string, object> { ["fetched"] = 0, ["stored"] = 0 });
            return Result<IngestTraceEventsResult>.Success(new IngestTraceEventsResult { Fetched = 0, Stored = 0 });
        }

        // Convert to domain entities
        var domainEvents = new List<TraceEvent>(pending.Count);
        foreach (var input in pending)
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
                _telemetry.LogWarn(OperationName, ctx.CorrelationId,
                    $"Skipping invalid trace event '{input.Id}': {ex.Message}");
            }
        }

        if (domainEvents.Count == 0)
        {
            span.SetResult("success");
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "success"
            });
            _telemetry.LogInfo(OperationName, ctx.CorrelationId,
                "All fetched events were invalid, none stored.",
                new Dictionary<string, object> { ["fetched"] = pending.Count, ["stored"] = 0 });
            return Result<IngestTraceEventsResult>.Success(
                new IngestTraceEventsResult { Fetched = pending.Count, Stored = 0 });
        }

        // Store in trace store
        var storeResult = await _traceStore.AppendAsync(domainEvents, ctx, ct);
        if (storeResult.IsFailure)
        {
            span.SetError(storeResult.Error.Code.ToString(), storeResult.Error.Message);
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "failure"
            });
            _telemetry.LogError(OperationName, ctx.CorrelationId,
                storeResult.Error.Message, storeResult.Error.Code.ToString(), storeResult.Error.Dependency);
            return Result<IngestTraceEventsResult>.Failure(storeResult.Error);
        }

        // Acknowledge successfully processed events
        var storedIds = domainEvents.Select(e => e.Id.Value).ToList();
        await _source.AcknowledgeAsync(storedIds, ctx, ct);

        span.SetResult("success");
        _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
        {
            ["operationName"] = OperationName,
            ["result"] = "success"
        });
        _telemetry.LogInfo(OperationName, ctx.CorrelationId,
            $"Ingested {storeResult.Value} trace events.",
            new Dictionary<string, object>
            {
                ["fetched"] = pending.Count,
                ["stored"] = storeResult.Value
            });

        return Result<IngestTraceEventsResult>.Success(
            new IngestTraceEventsResult { Fetched = pending.Count, Stored = storeResult.Value });
    }
}
