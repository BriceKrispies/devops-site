namespace DevOpsSite.Application.Authorization;

/// <summary>
/// A hard-stop override that blocks execution of a capability immediately,
/// regardless of all other resolution rules. Kill switches are checked first.
/// </summary>
public sealed record KillSwitch
{
    /// <summary>The OperationName of the capability to kill.</summary>
    public required string OperationName { get; init; }

    /// <summary>Whether the kill switch is currently active.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Human-readable reason why the kill switch was activated.</summary>
    public string? Reason { get; init; }

    /// <summary>Who activated the kill switch.</summary>
    public string? ActivatedBy { get; init; }

    /// <summary>When the kill switch was activated.</summary>
    public DateTimeOffset? ActivatedAt { get; init; }
}
