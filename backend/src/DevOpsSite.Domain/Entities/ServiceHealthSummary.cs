using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Entities;

/// <summary>
/// Normalized health summary for a known operational service.
/// Domain entity with invariants.
/// </summary>
public sealed class ServiceHealthSummary
{
    public ServiceId ServiceId { get; }
    public HealthStatus Status { get; }
    public string Description { get; }
    public DateTimeOffset CheckedAt { get; }

    private ServiceHealthSummary(ServiceId serviceId, HealthStatus status, string description, DateTimeOffset checkedAt)
    {
        ServiceId = serviceId;
        Status = status;
        Description = description;
        CheckedAt = checkedAt;
    }

    /// <summary>
    /// Factory enforcing invariants: Description cannot be null, CheckedAt must not be default.
    /// </summary>
    public static ServiceHealthSummary Create(ServiceId serviceId, HealthStatus status, string description, DateTimeOffset checkedAt)
    {
        if (serviceId is null)
            throw new ArgumentNullException(nameof(serviceId));
        if (description is null)
            throw new ArgumentNullException(nameof(description));
        if (checkedAt == default)
            throw new ArgumentException("CheckedAt must be a valid timestamp.", nameof(checkedAt));
        return new ServiceHealthSummary(serviceId, status, description, checkedAt);
    }
}
