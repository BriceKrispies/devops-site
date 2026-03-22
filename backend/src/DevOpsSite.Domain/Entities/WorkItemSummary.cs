using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Entities;

/// <summary>
/// Normalized work item summary from an external issue tracker.
/// Domain entity — no vendor concepts leak in.
/// </summary>
public sealed class WorkItemSummary
{
    public WorkItemKey Key { get; }
    public string Title { get; }
    public string Status { get; }
    public string? Category { get; }
    public string? Assignee { get; }
    public string? Url { get; }
    public string Provider { get; }
    public DateTimeOffset RetrievedAt { get; }

    private WorkItemSummary(WorkItemKey key, string title, string status, string? category,
        string? assignee, string? url, string provider, DateTimeOffset retrievedAt)
    {
        Key = key;
        Title = title;
        Status = status;
        Category = category;
        Assignee = assignee;
        Url = url;
        Provider = provider;
        RetrievedAt = retrievedAt;
    }

    public static WorkItemSummary Create(
        WorkItemKey key, string title, string status, string? category,
        string? assignee, string? url, string provider, DateTimeOffset retrievedAt)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status is required.", nameof(status));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));
        if (retrievedAt == default) throw new ArgumentException("RetrievedAt must be a valid timestamp.", nameof(retrievedAt));
        return new WorkItemSummary(key, title, status, category, assignee, url, provider, retrievedAt);
    }
}
