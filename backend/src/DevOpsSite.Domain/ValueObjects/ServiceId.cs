namespace DevOpsSite.Domain.ValueObjects;

/// <summary>
/// Identifies a known operational service. Value object: immutable, equality by value.
/// </summary>
public sealed record ServiceId
{
    public string Value { get; }

    private ServiceId(string value) => Value = value;

    public static ServiceId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ServiceId cannot be empty.", nameof(value));
        if (value.Length > 256)
            throw new ArgumentException("ServiceId cannot exceed 256 characters.", nameof(value));
        return new ServiceId(value.Trim());
    }
}
