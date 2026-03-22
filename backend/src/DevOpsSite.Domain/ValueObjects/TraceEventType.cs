namespace DevOpsSite.Domain.ValueObjects;

/// <summary>
/// Classifies a trace event (e.g. "deployment", "build", "incident", "alert", "change").
/// Value object: immutable, equality by value, normalized to lower-case.
/// </summary>
public sealed record TraceEventType
{
    public string Value { get; }

    private TraceEventType(string value) => Value = value;

    public static TraceEventType Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TraceEventType cannot be empty.", nameof(value));
        if (value.Length > 128)
            throw new ArgumentException("TraceEventType cannot exceed 128 characters.", nameof(value));
        return new TraceEventType(value.Trim().ToLowerInvariant());
    }
}
