using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Application.Tests.UseCases;

/// <summary>
/// Application behavior tests for IngestTraceEvents.
/// Constitution §6.2B: use case behavior, orchestration, error handling.
/// </summary>
public sealed class IngestTraceEventsTests
{
    private readonly InMemoryTraceStoreAdapter _traceStore = new();
    private readonly InMemoryTraceIngestionSourceAdapter _source = new();
    private readonly InMemoryTelemetryAdapter _telemetry = new();
    private readonly IngestTraceEventsHandler _handler;

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    public IngestTraceEventsTests()
    {
        var registry = new CapabilityRegistry();
        registry.Register(IngestTraceEventsHandler.Descriptor);
        var authz = new AuthorizationService(registry);
        _handler = new IngestTraceEventsHandler(_source, _traceStore, _telemetry, authz);
    }

    private static OperationContext CreateContext() => new()
    {
        CorrelationId = "test-corr-ingest-001",
        OperationName = IngestTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.BackgroundJob,
        Actor = new ActorIdentity { Id = "worker:trace-ingestion", Type = ActorType.Service },
        Permissions = new HashSet<Permission> { Permission.WellKnown.TraceEventsIngest }
    };

    private static OperationContext UnauthenticatedContext() => new()
    {
        CorrelationId = "test-corr-ingest-002",
        OperationName = IngestTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.BackgroundJob
    };

    private static OperationContext NoPermissionContext() => new()
    {
        CorrelationId = "test-corr-ingest-003",
        OperationName = IngestTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.BackgroundJob,
        Actor = new ActorIdentity { Id = "user-nope", Type = ActorType.User },
        Permissions = new HashSet<Permission>()
    };

    private void SeedPending(string id = "evt-1", string sourceSystem = "ci", string eventType = "build")
    {
        _source.Enqueue(new TraceEventInput
        {
            Id = id,
            SourceSystem = sourceSystem,
            EventType = eventType,
            OccurredAt = FixedTime,
            DisplayTitle = $"Event {id}",
            ServiceName = "api-service"
        });
    }

    [Fact]
    public async Task Should_fetch_and_store_pending_events()
    {
        SeedPending("evt-1");
        SeedPending("evt-2");

        var result = await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Fetched);
        Assert.Equal(2, result.Value.Stored);
        Assert.Equal(2, _traceStore.GetAll().Count);
    }

    [Fact]
    public async Task Should_acknowledge_stored_events()
    {
        SeedPending("evt-ack");

        await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.Contains("evt-ack", _source.GetAcknowledged());
    }

    [Fact]
    public async Task Should_return_zero_when_source_is_empty()
    {
        var result = await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Fetched);
        Assert.Equal(0, result.Value.Stored);
    }

    [Fact]
    public async Task Should_skip_invalid_events_and_store_valid_ones()
    {
        SeedPending("evt-good");
        _source.Enqueue(new TraceEventInput
        {
            Id = "", // invalid
            SourceSystem = "ci",
            EventType = "build",
            OccurredAt = FixedTime,
            DisplayTitle = "Bad event"
        });

        var result = await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Fetched);
        Assert.Equal(1, result.Value.Stored);
    }

    [Fact]
    public async Task Should_reject_invalid_batch_size()
    {
        var result = await _handler.HandleAsync(
            new IngestTraceEventsCommand { MaxBatchSize = 0 }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_invariant_violation_for_missing_correlation_id()
    {
        var badCtx = new OperationContext
        {
            CorrelationId = "",
            OperationName = IngestTraceEventsHandler.OperationName,
            Timestamp = FixedTime,
            Source = OperationSource.BackgroundJob
        };

        var result = await _handler.HandleAsync(new IngestTraceEventsCommand(), badCtx);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvariantViolation, result.Error.Code);
    }

    // --- Authorization tests ---

    [Fact]
    public async Task Should_return_unauthenticated_when_no_actor()
    {
        var result = await _handler.HandleAsync(new IngestTraceEventsCommand(), UnauthenticatedContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Authentication required", result.Error.Message);
    }

    [Fact]
    public async Task Should_return_forbidden_when_missing_permission()
    {
        var result = await _handler.HandleAsync(new IngestTraceEventsCommand(), NoPermissionContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Missing required permission", result.Error.Message);
    }

    // --- Observability assertion tests ---

    [Fact]
    public async Task Should_emit_success_span_on_successful_ingestion()
    {
        SeedPending();
        await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == IngestTraceEventsHandler.OperationName && s.Result == "success");
    }

    [Fact]
    public async Task Should_increment_success_counter()
    {
        await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_emit_structured_log_on_success()
    {
        SeedPending();
        await _handler.HandleAsync(new IngestTraceEventsCommand(), CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Info"
            && l.OperationName == IngestTraceEventsHandler.OperationName
            && l.CorrelationId == "test-corr-ingest-001");
    }
}
