namespace DevOpsSite.Domain.ValueObjects;

/// <summary>
/// Unique identifier for a normalized trace event. Value object: immutable, equality by value.
/// </summary>
public sealed record TraceEventId
{
    public string Value { get; }

    private TraceEventId(string value) => Value = value;

    public static TraceEventId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TraceEventId cannot be empty.", nameof(value));
        if (value.Length > 512)
            throw new ArgumentException("TraceEventId cannot exceed 512 characters.", nameof(value));
        return new TraceEventId(value.Trim());
    }
}
