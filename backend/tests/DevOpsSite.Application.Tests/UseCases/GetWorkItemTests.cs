using DevOpsSite.Adapters.Jira;
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.Tests.UseCases;

/// <summary>
/// Application behavior tests for GetWorkItem.
/// Constitution §6.2B: use case behavior, orchestration, error handling.
/// Constitution §14: authorization enforcement tests.
/// Tests behavior through faked ports.
/// </summary>
public sealed class GetWorkItemTests
{
    private readonly FakeWorkItemAdapter _workItemPort = new();
    private readonly InMemoryTelemetryAdapter _telemetry = new();
    private readonly GetWorkItemHandler _handler;

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    public GetWorkItemTests()
    {
        var registry = new CapabilityRegistry();
        registry.Register(GetWorkItemHandler.Descriptor);
        var authz = new AuthorizationService(registry);
        _handler = new GetWorkItemHandler(_workItemPort, _telemetry, authz);
    }

    private static OperationContext CreateContext() => new()
    {
        CorrelationId = "test-corr-wi-001",
        OperationName = GetWorkItemHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-1", Type = ActorType.User, DisplayName = "Test User" },
        Permissions = new HashSet<Permission> { Permission.WellKnown.WorkItemRead }
    };

    private static OperationContext UnauthenticatedContext() => new()
    {
        CorrelationId = "test-corr-wi-002",
        OperationName = GetWorkItemHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    private static OperationContext NoPermissionContext() => new()
    {
        CorrelationId = "test-corr-wi-003",
        OperationName = GetWorkItemHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-2", Type = ActorType.User, DisplayName = "No-Perms User" },
        Permissions = new HashSet<Permission>()
    };

    private void SeedItem(string key = "PROJ-100", string title = "Fix login", string status = "Open")
    {
        var item = WorkItemSummary.Create(
            WorkItemKey.Create(key), title, status,
            "Bug", "Alice", "https://jira.example.com/PROJ-100", "jira", FixedTime);
        _workItemPort.Seed(item);
    }

    [Fact]
    public async Task Should_return_work_item_when_exists()
    {
        SeedItem();

        var result = await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal("PROJ-100", result.Value.Key.Value);
        Assert.Equal("Fix login", result.Value.Title);
    }

    [Fact]
    public async Task Should_return_not_found_when_key_does_not_exist()
    {
        var result = await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "NOPE-999" }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_return_validation_error_for_empty_key(string? key)
    {
        var result = await _handler.HandleAsync(
            new GetWorkItemQuery { Key = key! }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_invariant_violation_for_missing_correlation_id()
    {
        var badCtx = new OperationContext
        {
            CorrelationId = "",
            OperationName = GetWorkItemHandler.OperationName,
            Timestamp = FixedTime,
            Source = OperationSource.HttpRequest
        };

        var result = await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, badCtx);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvariantViolation, result.Error.Code);
    }

    // --- Authorization tests (Constitution §14) ---

    [Fact]
    public async Task Should_return_unauthenticated_when_no_actor()
    {
        SeedItem();

        var result = await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, UnauthenticatedContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Authentication required", result.Error.Message);
    }

    [Fact]
    public async Task Should_return_forbidden_when_missing_permission()
    {
        SeedItem();

        var result = await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, NoPermissionContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Missing required permission", result.Error.Message);
    }

    // --- Observability assertion tests (Constitution §7.6) ---

    [Fact]
    public async Task Should_emit_success_span_on_successful_query()
    {
        SeedItem();

        await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == GetWorkItemHandler.OperationName && s.Result == "success");
    }

    [Fact]
    public async Task Should_emit_error_span_on_failure()
    {
        await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "MISSING-1" }, CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == GetWorkItemHandler.OperationName && s.ErrorCode == ErrorCode.NotFound.ToString());
    }

    [Fact]
    public async Task Should_increment_success_counter_on_success()
    {
        SeedItem();

        await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_increment_failure_counter_on_failure()
    {
        await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "MISSING-1" }, CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "failure");
    }

    [Fact]
    public async Task Should_emit_structured_log_on_success()
    {
        SeedItem();

        await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "PROJ-100" }, CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Info"
            && l.OperationName == GetWorkItemHandler.OperationName
            && l.CorrelationId == "test-corr-wi-001");
    }

    [Fact]
    public async Task Should_emit_structured_error_log_on_failure()
    {
        await _handler.HandleAsync(
            new GetWorkItemQuery { Key = "MISSING-1" }, CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Error"
            && l.OperationName == GetWorkItemHandler.OperationName
            && l.ErrorCode == ErrorCode.NotFound.ToString());
    }
}
