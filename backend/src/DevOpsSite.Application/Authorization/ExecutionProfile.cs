namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Internal execution profile seam for future AWS IAM role mapping.
/// Each profile represents a class of backend execution context with
/// specific infrastructure access requirements.
///
/// Today: internal metadata only.
/// Future: maps to AWS IAM role assumption, credential scoping, or
/// execution context selection at the adapter layer.
///
/// Do NOT couple these values to raw AWS IAM policy strings.
/// The mapping from ExecutionProfile to actual IAM roles will live
/// in the adapter layer when AWS integration is implemented.
/// </summary>
public enum ExecutionProfile
{
    /// <summary>No special infrastructure access. Internal operations only.</summary>
    Default,

    /// <summary>Read-only access to AWS resources. Safe for any viewer.</summary>
    ReadOnly,

    /// <summary>SQS/queue read and management operations (inspect, redrive, purge).</summary>
    QueueOperator,

    /// <summary>Database read and management operations (inspect, clone, snapshot).</summary>
    DatabaseOperator,

    /// <summary>Full administrative access. Reserved for cross-cutting operations.</summary>
    Admin
}
