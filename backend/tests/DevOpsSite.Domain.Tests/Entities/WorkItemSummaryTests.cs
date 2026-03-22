using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.Entities;

/// <summary>
/// Domain specification tests for WorkItemSummary entity.
/// Constitution §6.2A: invariants, factory validation, edge cases.
/// </summary>
public sealed class WorkItemSummaryTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly WorkItemKey ValidKey = WorkItemKey.Create("PROJ-100");

    [Fact]
    public void Should_create_valid_summary()
    {
        var summary = WorkItemSummary.Create(ValidKey, "Fix login bug", "In Progress",
            "Bug", "Alice", "https://jira.example.com/PROJ-100", "jira", FixedTime);

        Assert.Equal("PROJ-100", summary.Key.Value);
        Assert.Equal("Fix login bug", summary.Title);
        Assert.Equal("In Progress", summary.Status);
        Assert.Equal("Bug", summary.Category);
        Assert.Equal("Alice", summary.Assignee);
        Assert.Equal("jira", summary.Provider);
        Assert.Equal(FixedTime, summary.RetrievedAt);
    }

    [Fact]
    public void Should_allow_null_optional_fields()
    {
        var summary = WorkItemSummary.Create(ValidKey, "Task", "Open",
            null, null, null, "jira", FixedTime);

        Assert.Null(summary.Category);
        Assert.Null(summary.Assignee);
        Assert.Null(summary.Url);
    }

    [Fact]
    public void Should_reject_null_key()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkItemSummary.Create(null!, "Title", "Open", null, null, null, "jira", FixedTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_title(string? title)
    {
        Assert.Throws<ArgumentException>(() =>
            WorkItemSummary.Create(ValidKey, title!, "Open", null, null, null, "jira", FixedTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_status(string? status)
    {
        Assert.Throws<ArgumentException>(() =>
            WorkItemSummary.Create(ValidKey, "Title", status!, null, null, null, "jira", FixedTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_provider(string? provider)
    {
        Assert.Throws<ArgumentException>(() =>
            WorkItemSummary.Create(ValidKey, "Title", "Open", null, null, null, provider!, FixedTime));
    }

    [Fact]
    public void Should_reject_default_retrieved_at()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkItemSummary.Create(ValidKey, "Title", "Open", null, null, null, "jira", default));
    }
}
