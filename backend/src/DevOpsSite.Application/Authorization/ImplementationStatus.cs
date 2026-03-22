namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Implementation lifecycle status for a capability.
/// Capabilities progress through these states as they are built out.
/// </summary>
public enum ImplementationStatus
{
    /// <summary>Reserved in the catalog. No handler exists yet.</summary>
    Planned,

    /// <summary>Handler exists but returns NotImplemented. Useful for contract testing.</summary>
    Stub,

    /// <summary>Fully implemented, tested, and ready for use.</summary>
    Ready,

    /// <summary>Previously implemented but intentionally disabled (e.g., during incident).</summary>
    Disabled
}
