using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.Capabilities;

/// <summary>
/// In-memory implementation of the capability override store.
/// Suitable for local development and testing. In production, swap for
/// Redis, a config service, or database-backed implementation.
/// Thread-safe via lock.
/// </summary>
public sealed class InMemoryCapabilityOverrideStore : ICapabilityOverrideStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, KillSwitch> _killSwitches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CapabilityOverride> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public KillSwitch? GetKillSwitch(string operationName)
    {
        lock (_lock)
        {
            _killSwitches.TryGetValue(operationName, out var ks);
            return ks;
        }
    }

    public IReadOnlyList<KillSwitch> GetAllKillSwitches()
    {
        lock (_lock)
        {
            return _killSwitches.Values.ToList();
        }
    }

    public CapabilityOverride? GetOverride(string operationName)
    {
        lock (_lock)
        {
            _overrides.TryGetValue(operationName, out var ov);
            return ov;
        }
    }

    public IReadOnlyList<CapabilityOverride> GetAllOverrides()
    {
        lock (_lock)
        {
            return _overrides.Values.ToList();
        }
    }

    public void SetKillSwitch(KillSwitch killSwitch)
    {
        ArgumentNullException.ThrowIfNull(killSwitch);
        lock (_lock)
        {
            _killSwitches[killSwitch.OperationName] = killSwitch;
        }
    }

    public void SetOverride(CapabilityOverride capabilityOverride)
    {
        ArgumentNullException.ThrowIfNull(capabilityOverride);
        lock (_lock)
        {
            _overrides[capabilityOverride.OperationName] = capabilityOverride;
        }
    }

    public void RemoveOverride(string operationName)
    {
        lock (_lock)
        {
            _overrides.Remove(operationName);
        }
    }
}
