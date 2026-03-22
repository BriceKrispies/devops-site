using DevOpsSite.Adapters.ServiceHealth;
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.Tests.UseCases;

/// <summary>
/// Application behavior tests for GetServiceHealth.
/// Constitution §6.2B: use case behavior, orchestration, error handling.
/// Constitution §14: authorization enforcement tests.
/// Does not test implementation trivia. Tests behavior through faked ports.
/// </summary>
public sealed class GetServiceHealthTests
{
    private readonly FakeServiceHealthAdapter _healthPort = new();
    private readonly InMemoryTelemetryAdapter _telemetry = new();
    private readonly GetServiceHealthHandler _handler;

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    public GetServiceHealthTests()
    {
        var registry = new CapabilityRegistry();
        registry.Register(GetServiceHealthHandler.Descriptor);
        var authz = new AuthorizationService(registry);
        _handler = new GetServiceHealthHandler(_healthPort, _telemetry, authz);
    }

    private static OperationContext CreateContext() => new()
    {
        CorrelationId = "test-corr-001",
        OperationName = GetServiceHealthHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-1", Type = ActorType.User, DisplayName = "Test User" },
        Permissions = new HashSet<Permission> { Permission.WellKnown.ServiceHealthRead }
    };

    private static OperationContext UnauthenticatedContext() => new()
    {
        CorrelationId = "test-corr-002",
        OperationName = GetServiceHealthHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    private static OperationContext NoPermissionContext() => new()
    {
        CorrelationId = "test-corr-003",
        OperationName = GetServiceHealthHandler.OperationName,
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest,
        Actor = new ActorIdentity { Id = "user-2", Type = ActorType.User, DisplayName = "No-Perms" },
        Permissions = new HashSet<Permission>()
    };

    [Fact]
    public async Task Should_return_health_when_service_exists()
    {
        var summary = ServiceHealthSummary.Create(
            ServiceId.Create("api-gateway"), HealthStatus.Healthy, "All systems operational", FixedTime);
        _healthPort.Seed(summary);

        var result = await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "api-gateway" }, CreateContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(HealthStatus.Healthy, result.Value.Status);
        Assert.Equal("api-gateway", result.Value.ServiceId.Value);
    }

    [Fact]
    public async Task Should_return_not_found_when_service_does_not_exist()
    {
        var result = await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "nonexistent" }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_return_validation_error_for_empty_service_id(string? serviceId)
    {
        var result = await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = serviceId! }, CreateContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Validation, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_invariant_violation_for_missing_correlation_id()
    {
        var badCtx = new OperationContext
        {
            CorrelationId = "",
            OperationName = GetServiceHealthHandler.OperationName,
            Timestamp = FixedTime,
            Source = OperationSource.HttpRequest
        };

        var result = await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "svc" }, badCtx);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.InvariantViolation, result.Error.Code);
    }

    // --- Authorization tests (Constitution §14) ---

    [Fact]
    public async Task Should_return_unauthenticated_when_no_actor()
    {
        var result = await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "svc" }, UnauthenticatedContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Authentication required", result.Error.Message);
    }

    [Fact]
    public async Task Should_return_forbidden_when_missing_permission()
    {
        var result = await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "svc" }, NoPermissionContext());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
        Assert.Contains("Missing required permission", result.Error.Message);
    }

    // --- Observability assertion tests (Constitution §7.6) ---

    [Fact]
    public async Task Should_emit_success_span_on_successful_query()
    {
        var summary = ServiceHealthSummary.Create(
            ServiceId.Create("svc-1"), HealthStatus.Healthy, "ok", FixedTime);
        _healthPort.Seed(summary);

        await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "svc-1" }, CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == GetServiceHealthHandler.OperationName && s.Result == "success");
    }

    [Fact]
    public async Task Should_emit_error_span_on_failure()
    {
        await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "missing" }, CreateContext());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == GetServiceHealthHandler.OperationName && s.ErrorCode == ErrorCode.NotFound.ToString());
    }

    [Fact]
    public async Task Should_increment_success_counter_on_success()
    {
        var summary = ServiceHealthSummary.Create(
            ServiceId.Create("svc-1"), HealthStatus.Degraded, "slow", FixedTime);
        _healthPort.Seed(summary);

        await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "svc-1" }, CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_increment_failure_counter_on_failure()
    {
        await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "missing" }, CreateContext());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "failure");
    }

    [Fact]
    public async Task Should_emit_structured_log_on_success()
    {
        var summary = ServiceHealthSummary.Create(
            ServiceId.Create("svc-1"), HealthStatus.Healthy, "ok", FixedTime);
        _healthPort.Seed(summary);

        await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "svc-1" }, CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Info"
            && l.OperationName == GetServiceHealthHandler.OperationName
            && l.CorrelationId == "test-corr-001");
    }

    [Fact]
    public async Task Should_emit_structured_error_log_on_failure()
    {
        await _handler.HandleAsync(
            new GetServiceHealthQuery { ServiceId = "missing" }, CreateContext());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Error"
            && l.OperationName == GetServiceHealthHandler.OperationName
            && l.ErrorCode == ErrorCode.NotFound.ToString());
    }
}
