using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Entities;

/// <summary>
/// A normalized trace event from any source system (CI, alerting, deployment, etc.).
/// Domain entity with invariants. No vendor concepts leak in.
/// </summary>
public sealed class TraceEvent
{
    public TraceEventId Id { get; }
    public string SourceSystem { get; }
    public TraceEventType EventType { get; }
    public DateTimeOffset OccurredAt { get; }
    public string DisplayTitle { get; }
    public IReadOnlyList<string> Tags { get; }
    public string? ServiceName { get; }
    public IReadOnlyDictionary<string, string> RelatedIdentifiers { get; }
    public string? SourceUrl { get; }
    public string Provenance { get; }

    private TraceEvent(
        TraceEventId id,
        string sourceSystem,
        TraceEventType eventType,
        DateTimeOffset occurredAt,
        string displayTitle,
        IReadOnlyList<string> tags,
        string? serviceName,
        IReadOnlyDictionary<string, string> relatedIdentifiers,
        string? sourceUrl,
        string provenance)
    {
        Id = id;
        SourceSystem = sourceSystem;
        EventType = eventType;
        OccurredAt = occurredAt;
        DisplayTitle = displayTitle;
        Tags = tags;
        ServiceName = serviceName;
        RelatedIdentifiers = relatedIdentifiers;
        SourceUrl = sourceUrl;
        Provenance = provenance;
    }

    public static TraceEvent Create(
        TraceEventId id,
        string sourceSystem,
        TraceEventType eventType,
        DateTimeOffset occurredAt,
        string displayTitle,
        IReadOnlyList<string>? tags = null,
        string? serviceName = null,
        IReadOnlyDictionary<string, string>? relatedIdentifiers = null,
        string? sourceUrl = null,
        string? provenance = null)
    {
        if (id is null) throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(sourceSystem)) throw new ArgumentException("SourceSystem is required.", nameof(sourceSystem));
        if (eventType is null) throw new ArgumentNullException(nameof(eventType));
        if (occurredAt == default) throw new ArgumentException("OccurredAt must be a valid timestamp.", nameof(occurredAt));
        if (string.IsNullOrWhiteSpace(displayTitle)) throw new ArgumentException("DisplayTitle is required.", nameof(displayTitle));
        if (sourceSystem.Length > 128) throw new ArgumentException("SourceSystem cannot exceed 128 characters.", nameof(sourceSystem));
        if (displayTitle.Length > 512) throw new ArgumentException("DisplayTitle cannot exceed 512 characters.", nameof(displayTitle));

        return new TraceEvent(
            id,
            sourceSystem.Trim(),
            eventType,
            occurredAt,
            displayTitle.Trim(),
            tags ?? Array.Empty<string>(),
            serviceName?.Trim(),
            relatedIdentifiers ?? new Dictionary<string, string>(),
            sourceUrl?.Trim(),
            provenance ?? sourceSystem.Trim());
    }
}
