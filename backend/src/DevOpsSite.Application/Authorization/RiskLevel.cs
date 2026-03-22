namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Risk classification for operational capabilities.
/// Determines audit, approval, and execution constraints.
/// </summary>
public enum RiskLevel
{
    /// <summary>Read-only, no side effects. Safe to retry.</summary>
    Low,

    /// <summary>Mutating but bounded. May change operational state within one system.</summary>
    Medium,

    /// <summary>Mutating across systems or environments. Requires audit. May require approval.</summary>
    High,

    /// <summary>Irreversible or cross-environment destructive. Requires audit and explicit approval.</summary>
    Critical
}
