namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Stable internal permission representation.
/// Decoupled from identity-provider-specific claim names.
/// Constitution §14: Authorization primitives live in Application layer.
/// </summary>
public sealed record Permission
{
    public string Value { get; }

    private Permission(string value) => Value = value;

    public static Permission Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Permission value cannot be empty.", nameof(value));
        return new Permission(value.Trim().ToLowerInvariant());
    }

    /// <summary>Well-known permissions. Extend as capabilities grow.</summary>
    public static class WellKnown
    {
        // Existing — implemented
        public static readonly Permission WorkItemRead = Create("workitem:read");
        public static readonly Permission ServiceHealthRead = Create("servicehealth:read");
        public static readonly Permission TraceEventsRead = Create("traceevents:read");
        public static readonly Permission TraceEventsWrite = Create("traceevents:write");
        public static readonly Permission TraceEventsIngest = Create("traceevents:ingest");

        // Queues — planned (future AWS SQS)
        public static readonly Permission QueuesRead = Create("queues:read");
        public static readonly Permission QueuesOperate = Create("queues:operate");

        // Databases — planned (future AWS RDS)
        public static readonly Permission DatabasesRead = Create("databases:read");
        public static readonly Permission DatabasesOperate = Create("databases:operate");

        // Logs — planned (future CloudWatch)
        public static readonly Permission LogsRead = Create("logs:read");
    }
}
