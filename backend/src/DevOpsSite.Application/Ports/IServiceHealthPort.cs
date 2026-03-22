using DevOpsSite.Application.Context;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for retrieving service health from external monitoring systems.
/// Constitution §10: All external systems must be consumed through explicit ports.
/// </summary>
public interface IServiceHealthPort
{
    Task<Result<ServiceHealthSummary>> GetHealthAsync(ServiceId serviceId, OperationContext ctx, CancellationToken ct = default);
}
