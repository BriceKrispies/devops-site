using DevOpsSite.Application.Authorization;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for reading capability overrides and kill switches.
/// The backing store can be in-memory, Redis, a config file, etc.
/// </summary>
public interface ICapabilityOverrideStore
{
    /// <summary>Get the kill switch for a capability, or null if none exists.</summary>
    KillSwitch? GetKillSwitch(string operationName);

    /// <summary>Get all active kill switches.</summary>
    IReadOnlyList<KillSwitch> GetAllKillSwitches();

    /// <summary>Get the override for a capability, or null if none exists.</summary>
    CapabilityOverride? GetOverride(string operationName);

    /// <summary>Get all overrides.</summary>
    IReadOnlyList<CapabilityOverride> GetAllOverrides();

    /// <summary>Set or update a kill switch. Pass IsActive=false to deactivate.</summary>
    void SetKillSwitch(KillSwitch killSwitch);

    /// <summary>Set or update an override. Pass null to remove.</summary>
    void SetOverride(CapabilityOverride capabilityOverride);

    /// <summary>Remove an override for a capability.</summary>
    void RemoveOverride(string operationName);
}
