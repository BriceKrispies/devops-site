namespace DevOpsSite.Application.Authorization;

/// <summary>
/// The runtime-resolved status of a capability as seen by the frontend.
/// This is the result of the resolution pipeline (kill switch → environment → implementation → auth → override → default).
/// </summary>
public enum ResolvedCapabilityStatus
{
    /// <summary>Fully available to the current user.</summary>
    Enabled,

    /// <summary>Explicitly turned off (kill switch, admin override, or environment restriction).</summary>
    Disabled,

    /// <summary>Should not be rendered at all (hidden from the current user).</summary>
    Hidden,

    /// <summary>Visible but actions are blocked (user can see data but not mutate).</summary>
    ReadOnly,

    /// <summary>Partially available (dependency issues, reduced functionality).</summary>
    Degraded
}
