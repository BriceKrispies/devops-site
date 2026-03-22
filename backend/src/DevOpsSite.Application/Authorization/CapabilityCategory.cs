namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Functional category for operational capabilities.
/// Groups related capabilities for discovery and organization.
/// </summary>
public enum CapabilityCategory
{
    /// <summary>Trace event operations (read, write, ingest).</summary>
    Traces,

    /// <summary>Service health monitoring operations.</summary>
    ServiceHealth,

    /// <summary>Work item / issue tracker operations.</summary>
    WorkItems,

    /// <summary>Queue inspection and management operations (SQS, etc.).</summary>
    Queues,

    /// <summary>Database inspection and management operations.</summary>
    Databases,

    /// <summary>Log and metric viewing operations (CloudWatch, etc.).</summary>
    Logs,

    /// <summary>Administrative / cross-cutting operations.</summary>
    Admin
}
