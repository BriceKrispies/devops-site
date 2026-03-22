namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Authorization and operational metadata for a capability.
/// Every capability must declare one of these. Missing declaration = build/startup failure.
/// Constitution §14: Deny by default. Capabilities must declare auth requirements.
/// </summary>
public sealed record CapabilityDescriptor
{
    /// <summary>Unique operation name matching the handler's OperationName constant.</summary>
    public required string OperationName { get; init; }

    /// <summary>Whether this capability requires an authenticated actor. Default: true (deny by default).</summary>
    public bool RequiresAuthentication { get; init; } = true;

    /// <summary>Permissions required. Empty means authenticated-only, no specific permission.</summary>
    public IReadOnlyList<Permission> RequiredPermissions { get; init; } = [];

    /// <summary>Whether this is a privileged/mutating action (requires audit).</summary>
    public bool IsPrivileged { get; init; }

    /// <summary>Whether audit events must be emitted for this capability.</summary>
    public bool RequiresAudit { get; init; }

    /// <summary>Human-readable description for documentation/debugging.</summary>
    public string? Description { get; init; }

    // --- Capability shell metadata (for catalog/registry enrichment) ---

    /// <summary>Functional category. Default: Traces (backward-compatible for existing capabilities).</summary>
    public CapabilityCategory Category { get; init; } = CapabilityCategory.Traces;

    /// <summary>Risk classification. Determines audit, approval, and execution constraints.</summary>
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Low;

    /// <summary>Synchronous or asynchronous execution model.</summary>
    public ExecutionMode ExecutionMode { get; init; } = ExecutionMode.Synchronous;

    /// <summary>Implementation lifecycle status. Planned capabilities have no handler yet.</summary>
    public ImplementationStatus Status { get; init; } = ImplementationStatus.Ready;

    /// <summary>
    /// Execution profile seam for future AWS IAM role mapping.
    /// Today: internal metadata. Future: drives adapter-layer credential scoping.
    /// </summary>
    public ExecutionProfile ExecutionProfile { get; init; } = ExecutionProfile.Default;

    /// <summary>
    /// Validates the descriptor for consistency.
    /// Returns error messages for violations.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(OperationName))
            errors.Add("OperationName is required.");

        if (IsPrivileged && !RequiresAudit)
            errors.Add($"Privileged capability '{OperationName}' must require audit.");

        if (IsPrivileged && !RequiresAuthentication)
            errors.Add($"Privileged capability '{OperationName}' must require authentication.");

        if (!RequiresAuthentication && RequiredPermissions.Count > 0)
            errors.Add($"Public capability '{OperationName}' cannot require permissions.");

        if (RiskLevel >= RiskLevel.High && !RequiresAudit)
            errors.Add($"High/Critical risk capability '{OperationName}' must require audit.");

        if (RiskLevel >= RiskLevel.High && !RequiresAuthentication)
            errors.Add($"High/Critical risk capability '{OperationName}' must require authentication.");

        return errors;
    }
}
