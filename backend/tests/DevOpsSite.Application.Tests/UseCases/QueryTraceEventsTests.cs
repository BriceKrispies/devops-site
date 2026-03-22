using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.Tests.UseCases;

/// <summary>
/// Application behavior tests for QueryTraceEvents.
/// Constitution §6.2B: use case behavior, orchestration, error handling.
/// </summary>
public sealed class QueryTraceEventsTests
{
    private readonly InMemoryTraceStoreAdapter _traceStore = new();
    private readonly InMemoryTelemetryAdapter _telemetry = new();
    private readonly QueryTraceEventsHandler _handler;

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    public QueryTraceEventsTests()
    {
        var registry = new CapabilityRegistry();
        registry.Register(QueryTraceEventsHandler.Descriptor);
        var authz = new AuthorizationService(registry);
        _handler = new QueryTraceEventsHandler(_traceStore, _telemetry, authz);
    }

    private static OperationContext CreateContext() => new()
    {
        CorrelationId = "test-corr-qte-001",
        OperationName = QueryTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-1", Type = ActorType.User, DisplayName = "Test User" },
        Permissions = new HashSet<Permission> { Permission.WellKnown.TraceEventsRead }
    };

    private static OperationContext UnauthenticatedContext() => new()
    {
        CorrelationId = "test-corr-qte-002",
        OperationName = QueryTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    private static OperationContext NoPermissionContext() => new()
    {
        CorrelationId = "test-corr-qte-003",
        OperationName = QueryTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-2", Type = ActorType.User },
        Permissions = new HashSet<Permission>()
    };

    private void SeedEvents()
    {
        _traceStore.Seed(TraceEvent.Create(
            TraceEventId.Create("evt-1"), "github-actions",
            TraceEventType.Create("deployment"), FixedTime,
            "Deploy to prod", serviceName: "api-service"));
        _traceStore.Seed(TraceEvent.Create(
            TraceEventId.Create("evt-2"), "pagerduty",
            TraceEventType.Create("incident"),
            FixedTime.AddHours(1),
            "High latency alert", serviceName: "web-service"));
    }

    [Fact]
    public async Task Should_return_all_events_with_no_filter()
    {
        SeedEvents();

        var result = await _handler.HandleAsync(new QueryTraceEventsQuery(), CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Should_filter_by_service_name()
    {
        SeedEvents();

        var result = await _handler.HandleAsync(
            new QueryTraceEventsQuery { ServiceName = "api-service" }, CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("api-service", result.Value[0].ServiceName);
    }

    [Fact]
    public async Task Should_filter_by_event_type()
    {
        SeedEvents();

        var result = await _handler.HandleAsync(
            new QueryTraceEventsQuery { EventType = "incident" }, CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("incident", result.Value[0].EventType.Value);
    }

    [Fact]
    public async Task Should_return_empty_when_no_matches()
    {
        var result = await _handler.HandleAsync(
            new QueryTraceEventsQuery { ServiceName = "nonexistent" }, CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Should_reject_invalid_limit()
    {
        var result = await _handler.HandleAsync(
            new QueryTraceEventsQuery { Limit = 0 }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_reject_from_after_to()
    {
        var result = await _handler.HandleAsync(
            new QueryTraceEventsQuery
            {
                From = FixedTime.AddDays(1),
                To = FixedTime
            }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_invariant_violation_for_missing_correlation_id()
    {
        var badCtx = new OperationContext
        {
            CorrelationId = "",
            OperationName = QueryTraceEventsHandler.OperationName,
            Timestamp = FixedTime,
            Source = OperationSource.HttpRequest
        };

        var result = await _handler.HandleAsync(new QueryTraceEventsQuery(), badCtx);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvariantViolation, result.Error.Code);
    }

    // --- Authorization tests ---

    [Fact]
    public async Task Should_return_unauthenticated_when_no_actor()
    {
        var result = await _handler.HandleAsync(new QueryTraceEventsQuery(), UnauthenticatedContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Authentication required", result.Error.Message);
    }

    [Fact]
    public async Task Should_return_forbidden_when_missing_permission()
    {
        var result = await _handler.HandleAsync(new QueryTraceEventsQuery(), NoPermissionContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Missing required permission", result.Error.Message);
    }

    // --- Observability assertion tests ---

    [Fact]
    public async Task Should_emit_success_span_on_successful_query()
    {
        SeedEvents();
        await _handler.HandleAsync(new QueryTraceEventsQuery(), CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == QueryTraceEventsHandler.OperationName && s.Result == "success");
    }

    [Fact]
    public async Task Should_increment_success_counter()
    {
        await _handler.HandleAsync(new QueryTraceEventsQuery(), CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_emit_structured_log_on_success()
    {
        await _handler.HandleAsync(new QueryTraceEventsQuery(), CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Info"
            && l.OperationName == QueryTraceEventsHandler.OperationName
            && l.CorrelationId == "test-corr-qte-001");
    }
}
