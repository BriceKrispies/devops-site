namespace DevOpsSite.Application.Audit;

/// <summary>
/// Audit event for privileged/operationally meaningful actions.
/// Constitution §7.5: Every privileged action must emit an audit record.
/// </summary>
public sealed record AuditEvent
{
    public required string EventId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Action { get; init; }
    public required string ActorId { get; init; }
    public required string ActorType { get; init; }
    public required string Target { get; init; }
    public required string Outcome { get; init; }
    public required string CorrelationId { get; init; }
    public string? TenantId { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyDictionary<string, string>? Detail { get; init; }
}
