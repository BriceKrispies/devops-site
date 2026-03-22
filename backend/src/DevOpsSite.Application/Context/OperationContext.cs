using DevOpsSite.Application.Authorization;

namespace DevOpsSite.Application.Context;

/// <summary>
/// Standard context carried by every command, query, and background operation.
/// Constitution §7.1: Every operation must carry context.
/// Constitution §14: Actor and permissions carried for authorization.
/// </summary>
public sealed record OperationContext
{
    public required string CorrelationId { get; init; }
    public required string OperationName { get; init; }
    public string? CausationId { get; init; }
    public ActorIdentity? Actor { get; init; }
    public string? TenantId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required OperationSource Source { get; init; }

    /// <summary>
    /// Normalized permissions resolved from the actor's identity/roles.
    /// Populated by the host middleware from the external identity provider.
    /// </summary>
    public IReadOnlySet<Permission> Permissions { get; init; } = new HashSet<Permission>();

    /// <summary>
    /// Whether the actor has been authenticated (token validated).
    /// </summary>
    public bool IsAuthenticated => Actor is not null;

    /// <summary>
    /// Validates that required fields are present. Returns error messages for violations.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(CorrelationId))
            errors.Add("CorrelationId is required.");
        if (string.IsNullOrWhiteSpace(OperationName))
            errors.Add("OperationName is required.");
        return errors;
    }
}

public sealed record ActorIdentity
{
    public required string Id { get; init; }
    public required ActorType Type { get; init; }
    public string? DisplayName { get; init; }
}

public enum ActorType
{
    User,
    Service,
    System,
    Scheduler
}

public enum OperationSource
{
    HttpRequest,
    BackgroundJob,
    QueueMessage,
    Scheduler,
    InternalWorkflow
}
