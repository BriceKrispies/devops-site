using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.ServiceHealth;

/// <summary>
/// Fake adapter for tests and local development. Returns configurable health data.
/// </summary>
public sealed class FakeServiceHealthAdapter : IServiceHealthPort
{
    private readonly Dictionary<string, ServiceHealthSummary> _data = new();

    public void Seed(ServiceHealthSummary summary) =>
        _data[summary.ServiceId.Value] = summary;

    public Task<Result<ServiceHealthSummary>> GetHealthAsync(ServiceId serviceId, OperationContext ctx, CancellationToken ct = default)
    {
        if (_data.TryGetValue(serviceId.Value, out var summary))
            return Task.FromResult(Result<ServiceHealthSummary>.Success(summary));

        return Task.FromResult(Result<ServiceHealthSummary>.Failure(
            AppError.NotFound(
                $"No health data found for service '{serviceId.Value}'.",
                ctx.OperationName,
                ctx.CorrelationId)));
    }
}
