using DevOpsSite.Adapters.ServiceHealth;
using DevOpsSite.Application.Ports;
using DevOpsSite.Contracts.Tests.ServiceHealth;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.Tests.ServiceHealth;

/// <summary>
/// Adapter test for FakeServiceHealthAdapter.
/// Constitution §6.2D: Adapter tested against contract suite.
/// </summary>
public sealed class FakeServiceHealthAdapterTests : ServiceHealthPortContractTests
{
    private readonly FakeServiceHealthAdapter _adapter = new();

    protected override IServiceHealthPort CreateAdapter() => _adapter;

    protected override Task SeedKnownService(string serviceId, HealthStatus status, string description)
    {
        var summary = ServiceHealthSummary.Create(
            ServiceId.Create(serviceId), status, description,
            new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));
        _adapter.Seed(summary);
        return Task.CompletedTask;
    }
}
