namespace DevOpsSite.Application.Authorization;

/// <summary>
/// A capability resolved for a specific user/session/environment.
/// This is the API contract returned to the frontend.
/// </summary>
public sealed record ResolvedCapability
{
    /// <summary>Unique capability identifier (matches OperationName).</summary>
    public required string Key { get; init; }

    /// <summary>Runtime-resolved status after all resolution rules applied.</summary>
    public required ResolvedCapabilityStatus Status { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Frontend feature area grouping.</summary>
    public required string Area { get; init; }

    /// <summary>One-line description of the capability.</summary>
    public required string Description { get; init; }

    /// <summary>Risk classification.</summary>
    public required string Risk { get; init; }

    /// <summary>Frontend route, or null if not navigable.</summary>
    public string? Route { get; init; }

    /// <summary>Optional user-facing message explaining the status.</summary>
    public string? Message { get; init; }

    /// <summary>Machine-readable reason for the status (kill_switch, not_implemented, forbidden, override, environment).</summary>
    public string? Reason { get; init; }

    /// <summary>Required permission strings (for frontend display).</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>Additional metadata useful for frontend rendering.</summary>
    public ResolvedCapabilityMetadata? Metadata { get; init; }
}

public sealed record ResolvedCapabilityMetadata
{
    public string? Category { get; init; }
    public bool Privileged { get; init; }
    public string? ExecutionMode { get; init; }
}
