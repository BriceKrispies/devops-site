namespace DevOpsSite.Application.Queries;

/// <summary>
/// Filter criteria for querying trace events.
/// All fields are optional — omitted fields are not filtered on.
/// </summary>
public sealed record TraceQuery
{
    /// <summary>Filter by service name (exact match).</summary>
    public string? ServiceName { get; init; }

    /// <summary>Filter by event type (exact match).</summary>
    public string? EventType { get; init; }

    /// <summary>Filter by source system (exact match).</summary>
    public string? SourceSystem { get; init; }

    /// <summary>Events that occurred at or after this time.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Events that occurred at or before this time.</summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>Maximum number of events to return. Default: 100.</summary>
    public int Limit { get; init; } = 100;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Limit < 1) errors.Add("Limit must be at least 1.");
        if (Limit > 1000) errors.Add("Limit cannot exceed 1000.");
        if (From.HasValue && To.HasValue && From > To)
            errors.Add("From must be before or equal to To.");
        return errors;
    }
}
