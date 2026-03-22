using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.Entities;

/// <summary>
/// Domain specification tests for ServiceHealthSummary.
/// Constitution §6.2A: invariants, state transitions, edge cases.
/// </summary>
public sealed class ServiceHealthSummaryTests
{
    private static readonly ServiceId ValidId = ServiceId.Create("test-service");
    private static readonly DateTimeOffset ValidTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_create_valid_summary()
    {
        var summary = ServiceHealthSummary.Create(ValidId, HealthStatus.Healthy, "All good", ValidTime);

        Assert.Equal(ValidId, summary.ServiceId);
        Assert.Equal(HealthStatus.Healthy, summary.Status);
        Assert.Equal("All good", summary.Description);
        Assert.Equal(ValidTime, summary.CheckedAt);
    }

    [Fact]
    public void Should_reject_null_service_id()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceHealthSummary.Create(null!, HealthStatus.Healthy, "ok", ValidTime));
    }

    [Fact]
    public void Should_reject_null_description()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceHealthSummary.Create(ValidId, HealthStatus.Healthy, null!, ValidTime));
    }

    [Fact]
    public void Should_reject_default_checked_at()
    {
        Assert.Throws<ArgumentException>(() =>
            ServiceHealthSummary.Create(ValidId, HealthStatus.Healthy, "ok", default));
    }

    [Theory]
    [InlineData(HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Unknown)]
    public void Should_accept_all_health_statuses(HealthStatus status)
    {
        var summary = ServiceHealthSummary.Create(ValidId, status, "desc", ValidTime);
        Assert.Equal(status, summary.Status);
    }
}
