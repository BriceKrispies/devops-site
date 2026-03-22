namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Central registry of all capability authorization descriptors.
/// Used by the authorization service and startup validation.
/// Constitution §14: Every capability must be registered. Missing registration = startup failure.
/// </summary>
public sealed class CapabilityRegistry
{
    private readonly Dictionary<string, CapabilityDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);

    public void Register(CapabilityDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var errors = descriptor.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Invalid capability descriptor '{descriptor.OperationName}': {string.Join("; ", errors)}");

        if (!_descriptors.TryAdd(descriptor.OperationName, descriptor))
            throw new InvalidOperationException(
                $"Capability '{descriptor.OperationName}' is already registered.");
    }

    public CapabilityDescriptor? GetDescriptor(string operationName)
    {
        _descriptors.TryGetValue(operationName, out var descriptor);
        return descriptor;
    }

    public IReadOnlyCollection<CapabilityDescriptor> GetAll() => _descriptors.Values;

    public IReadOnlyCollection<string> GetRegisteredOperations() => _descriptors.Keys;
}
