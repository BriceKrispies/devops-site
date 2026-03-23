namespace DevOpsSite.Application.Authorization;

/// <summary>
/// An explicit runtime override for a capability's resolved status.
/// Applied after kill switch, environment, and implementation checks but
/// before the default resolution.
/// </summary>
public sealed record CapabilityOverride
{
    /// <summary>The OperationName of the capability to override.</summary>
    public required string OperationName { get; init; }

    /// <summary>The status to force.</summary>
    public required ResolvedCapabilityStatus Status { get; init; }

    /// <summary>Human-readable reason for the override.</summary>
    public string? Reason { get; init; }

    /// <summary>Who set the override.</summary>
    public string? SetBy { get; init; }

    /// <summary>When the override was set.</summary>
    public DateTimeOffset? SetAt { get; init; }
}
