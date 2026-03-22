namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Central catalog of all operational capabilities — implemented and planned.
/// This is the single source of truth for what the system can and will be able to do.
///
/// Implemented capabilities (Status=Ready) have handlers and are registered in the
/// CapabilityRegistry for runtime authorization. Planned capabilities (Status=Planned)
/// reserve a slot with full metadata but have no handler yet.
///
/// Constitution §14: Deny by default. Every capability is classified before it ships.
/// </summary>
public static class OperationalCapabilityCatalog
{
    // ──────────────────────────────────────────────────────────────
    //  Trace capabilities (implemented)
    // ──────────────────────────────────────────────────────────────

    public static readonly CapabilityDescriptor QueryTraceEvents = new()
    {
        OperationName = "QueryTraceEvents",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.TraceEventsRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Query normalized trace events from the store.",
        Category = CapabilityCategory.Traces,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    public static readonly CapabilityDescriptor AddTraceEvents = new()
    {
        OperationName = "AddTraceEvents",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.TraceEventsWrite],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Append normalized trace events to the store.",
        Category = CapabilityCategory.Traces,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    public static readonly CapabilityDescriptor IngestTraceEvents = new()
    {
        OperationName = "IngestTraceEvents",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.TraceEventsIngest],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Background ingestion of trace events from external sources.",
        Category = CapabilityCategory.Traces,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    // ──────────────────────────────────────────────────────────────
    //  Service health capabilities (implemented)
    // ──────────────────────────────────────────────────────────────

    public static readonly CapabilityDescriptor GetServiceHealth = new()
    {
        OperationName = "GetServiceHealth",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.ServiceHealthRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Retrieve normalized health status for a known service.",
        Category = CapabilityCategory.ServiceHealth,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    // ──────────────────────────────────────────────────────────────
    //  Work item capabilities (implemented)
    // ──────────────────────────────────────────────────────────────

    public static readonly CapabilityDescriptor GetWorkItem = new()
    {
        OperationName = "GetWorkItem",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.WorkItemRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Retrieve a normalized work item summary by key.",
        Category = CapabilityCategory.WorkItems,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    // ──────────────────────────────────────────────────────────────
    //  Queue capabilities (planned — future AWS SQS integration)
    // ──────────────────────────────────────────────────────────────

    public static readonly CapabilityDescriptor QueuesRead = new()
    {
        OperationName = "QueuesRead",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.QueuesRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "List and inspect SQS queues and their metrics (depth, age, DLQ status).",
        Category = CapabilityCategory.Queues,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Planned,
        ExecutionProfile = ExecutionProfile.ReadOnly
    };

    public static readonly CapabilityDescriptor QueuesRedriveDlq = new()
    {
        OperationName = "QueuesRedriveDlq",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.QueuesOperate],
        IsPrivileged = true,
        RequiresAudit = true,
        Description = "Redrive messages from a dead-letter queue back to the source queue.",
        Category = CapabilityCategory.Queues,
        RiskLevel = RiskLevel.High,
        ExecutionMode = ExecutionMode.Asynchronous,
        Status = ImplementationStatus.Planned,
        ExecutionProfile = ExecutionProfile.QueueOperator
    };

    // ──────────────────────────────────────────────────────────────
    //  Database capabilities (planned — future AWS RDS integration)
    // ──────────────────────────────────────────────────────────────

    public static readonly CapabilityDescriptor DatabasesRead = new()
    {
        OperationName = "DatabasesRead",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.DatabasesRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "List and inspect database instances, their status, and basic metrics.",
        Category = CapabilityCategory.Databases,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Planned,
        ExecutionProfile = ExecutionProfile.ReadOnly
    };

    public static readonly CapabilityDescriptor DatabasesCloneNonProd = new()
    {
        OperationName = "DatabasesCloneNonProd",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.DatabasesOperate],
        IsPrivileged = true,
        RequiresAudit = true,
        Description = "Clone a database to a non-production environment for testing or debugging.",
        Category = CapabilityCategory.Databases,
        RiskLevel = RiskLevel.Critical,
        ExecutionMode = ExecutionMode.Asynchronous,
        Status = ImplementationStatus.Planned,
        ExecutionProfile = ExecutionProfile.DatabaseOperator
    };

    // ──────────────────────────────────────────────────────────────
    //  Log capabilities (planned — future CloudWatch integration)
    // ──────────────────────────────────────────────────────────────

    public static readonly CapabilityDescriptor LogsRead = new()
    {
        OperationName = "LogsRead",
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.LogsRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Query and view CloudWatch log groups and log streams.",
        Category = CapabilityCategory.Logs,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Planned,
        ExecutionProfile = ExecutionProfile.ReadOnly
    };

    // ──────────────────────────────────────────────────────────────
    //  Catalog access
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// All capabilities in the catalog, implemented and planned.
    /// </summary>
    public static IReadOnlyList<CapabilityDescriptor> All { get; } = new[]
    {
        // Implemented
        QueryTraceEvents,
        AddTraceEvents,
        IngestTraceEvents,
        GetServiceHealth,
        GetWorkItem,
        // Planned — AWS operational capabilities
        QueuesRead,
        QueuesRedriveDlq,
        DatabasesRead,
        DatabasesCloneNonProd,
        LogsRead
    };

    /// <summary>
    /// Only capabilities that are fully implemented and ready for runtime use.
    /// These should be registered in the CapabilityRegistry at startup.
    /// </summary>
    public static IReadOnlyList<CapabilityDescriptor> Implemented { get; } =
        All.Where(c => c.Status == ImplementationStatus.Ready).ToList();

    /// <summary>
    /// Only capabilities that are planned/reserved but not yet implemented.
    /// These are NOT registered in the CapabilityRegistry (no handler exists).
    /// </summary>
    public static IReadOnlyList<CapabilityDescriptor> Planned { get; } =
        All.Where(c => c.Status == ImplementationStatus.Planned).ToList();

    /// <summary>
    /// Get all capabilities in a given category.
    /// </summary>
    public static IReadOnlyList<CapabilityDescriptor> ByCategory(CapabilityCategory category) =>
        All.Where(c => c.Category == category).ToList();

    /// <summary>
    /// Get a capability by operation name, or null if not found.
    /// </summary>
    public static CapabilityDescriptor? GetByOperationName(string operationName) =>
        All.FirstOrDefault(c => string.Equals(c.OperationName, operationName, StringComparison.OrdinalIgnoreCase));
}
