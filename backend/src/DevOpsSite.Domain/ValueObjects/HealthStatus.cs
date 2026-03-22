namespace DevOpsSite.Domain.ValueObjects;

/// <summary>
/// Normalized health status of a service. Domain does not know vendor-specific health formats.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
