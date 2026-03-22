using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Application.Tests.UseCases;

/// <summary>
/// Application behavior tests for AddTraceEvents.
/// Constitution §6.2B: use case behavior, orchestration, error handling.
/// </summary>
public sealed class AddTraceEventsTests
{
    private readonly InMemoryTraceStoreAdapter _traceStore = new();
    private readonly InMemoryTelemetryAdapter _telemetry = new();
    private readonly AddTraceEventsHandler _handler;

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    public AddTraceEventsTests()
    {
        var registry = new CapabilityRegistry();
        registry.Register(AddTraceEventsHandler.Descriptor);
        var authz = new AuthorizationService(registry);
        _handler = new AddTraceEventsHandler(_traceStore, _telemetry, authz);
    }

    private static OperationContext CreateContext() => new()
    {
        CorrelationId = "test-corr-te-001",
        OperationName = AddTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-1", Type = ActorType.User, DisplayName = "Test User" },
        Permissions = new HashSet<Permission> { Permission.WellKnown.TraceEventsWrite }
    };

    private static OperationContext UnauthenticatedContext() => new()
    {
        CorrelationId = "test-corr-te-002",
        OperationName = AddTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    private static OperationContext NoPermissionContext() => new()
    {
        CorrelationId = "test-corr-te-003",
        OperationName = AddTraceEventsHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-2", Type = ActorType.User },
        Permissions = new HashSet<Permission>()
    };

    private static AddTraceEventsCommand ValidCommand() => new()
    {
        Events = new List<TraceEventInput>
        {
            new()
            {
                Id = "evt-1",
                SourceSystem = "github-actions",
                EventType = "deployment",
                OccurredAt = FixedTime,
                DisplayTitle = "Deploy to prod",
                ServiceName = "api-service"
            }
        }
    };

    [Fact]
    public async Task Should_append_events_successfully()
    {
        var result = await _handler.HandleAsync(ValidCommand(), CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Single(_traceStore.GetAll());
    }

    [Fact]
    public async Task Should_reject_empty_events_list()
    {
        var command = new AddTraceEventsCommand { Events = new List<TraceEventInput>() };

        var result = await _handler.HandleAsync(command, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_reject_invalid_trace_event()
    {
        var command = new AddTraceEventsCommand
        {
            Events = new List<TraceEventInput>
            {
                new()
                {
                    Id = "evt-1",
                    SourceSystem = "", // invalid
                    EventType = "deployment",
                    OccurredAt = FixedTime,
                    DisplayTitle = "Deploy"
                }
            }
        };

        var result = await _handler.HandleAsync(command, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_invariant_violation_for_missing_correlation_id()
    {
        var badCtx = new OperationContext
        {
            CorrelationId = "",
            OperationName = AddTraceEventsHandler.OperationName,
            Timestamp = FixedTime,
            Source = OperationSource.HttpRequest
        };

        var result = await _handler.HandleAsync(ValidCommand(), badCtx);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvariantViolation, result.Error.Code);
    }

    // --- Authorization tests ---

    [Fact]
    public async Task Should_return_unauthenticated_when_no_actor()
    {
        var result = await _handler.HandleAsync(ValidCommand(), UnauthenticatedContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Authentication required", result.Error.Message);
    }

    [Fact]
    public async Task Should_return_forbidden_when_missing_permission()
    {
        var result = await _handler.HandleAsync(ValidCommand(), NoPermissionContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Missing required permission", result.Error.Message);
    }

    // --- Observability assertion tests ---

    [Fact]
    public async Task Should_emit_success_span_on_successful_append()
    {
        await _handler.HandleAsync(ValidCommand(), CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == AddTraceEventsHandler.OperationName && s.Result == "success");
    }

    [Fact]
    public async Task Should_increment_success_counter()
    {
        await _handler.HandleAsync(ValidCommand(), CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_emit_structured_log_on_success()
    {
        await _handler.HandleAsync(ValidCommand(), CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Info"
            && l.OperationName == AddTraceEventsHandler.OperationName
            && l.CorrelationId == "test-corr-te-001");
    }
}
