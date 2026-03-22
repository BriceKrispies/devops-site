namespace DevOpsSite.Domain.ValueObjects;

/// <summary>
/// Normalized external work item identifier (e.g., "PROJ-123").
/// Value object: immutable, equality by value.
/// </summary>
public sealed record WorkItemKey
{
    public string Value { get; }

    private WorkItemKey(string value) => Value = value;

    public static WorkItemKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("WorkItemKey cannot be empty.", nameof(value));
        if (value.Length > 128)
            throw new ArgumentException("WorkItemKey cannot exceed 128 characters.", nameof(value));
        return new WorkItemKey(value.Trim().ToUpperInvariant());
    }
}
