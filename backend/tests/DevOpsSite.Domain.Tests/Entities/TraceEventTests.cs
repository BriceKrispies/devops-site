using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.Entities;

public sealed class TraceEventTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static TraceEvent CreateValid(
        string id = "evt-1",
        string sourceSystem = "github-actions",
        string eventType = "deployment",
        string displayTitle = "Deploy to prod") =>
        TraceEvent.Create(
            TraceEventId.Create(id),
            sourceSystem,
            TraceEventType.Create(eventType),
            FixedTime,
            displayTitle);

    [Fact]
    public void Should_create_with_required_fields()
    {
        var e = CreateValid();
        Assert.Equal("evt-1", e.Id.Value);
        Assert.Equal("github-actions", e.SourceSystem);
        Assert.Equal("deployment", e.EventType.Value);
        Assert.Equal(FixedTime, e.OccurredAt);
        Assert.Equal("Deploy to prod", e.DisplayTitle);
    }

    [Fact]
    public void Should_default_provenance_to_source_system()
    {
        var e = CreateValid();
        Assert.Equal("github-actions", e.Provenance);
    }

    [Fact]
    public void Should_accept_explicit_provenance()
    {
        var e = TraceEvent.Create(
            TraceEventId.Create("evt-1"), "github-actions",
            TraceEventType.Create("deployment"), FixedTime, "Deploy",
            provenance: "ci-ingest-job");
        Assert.Equal("ci-ingest-job", e.Provenance);
    }

    [Fact]
    public void Should_default_tags_to_empty()
    {
        var e = CreateValid();
        Assert.Empty(e.Tags);
    }

    [Fact]
    public void Should_default_related_identifiers_to_empty()
    {
        var e = CreateValid();
        Assert.Empty(e.RelatedIdentifiers);
    }

    [Fact]
    public void Should_accept_optional_fields()
    {
        var tags = new List<string> { "prod", "us-east-1" };
        var related = new Dictionary<string, string> { ["commitSha"] = "abc123" };

        var e = TraceEvent.Create(
            TraceEventId.Create("evt-2"), "github-actions",
            TraceEventType.Create("deployment"), FixedTime, "Deploy v2",
            tags, "api-service", related, "https://github.com/runs/1", "manual-ingest");

        Assert.Equal(tags, e.Tags);
        Assert.Equal("api-service", e.ServiceName);
        Assert.Equal("abc123", e.RelatedIdentifiers["commitSha"]);
        Assert.Equal("https://github.com/runs/1", e.SourceUrl);
        Assert.Equal("manual-ingest", e.Provenance);
    }

    [Fact]
    public void Should_reject_null_id()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TraceEvent.Create(null!, "src", TraceEventType.Create("t"), FixedTime, "Title"));
    }

    [Fact]
    public void Should_reject_null_event_type()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TraceEvent.Create(TraceEventId.Create("e"), "src", null!, FixedTime, "Title"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_source_system(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            TraceEvent.Create(TraceEventId.Create("e"), value!, TraceEventType.Create("t"), FixedTime, "Title"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_display_title(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            TraceEvent.Create(TraceEventId.Create("e"), "src", TraceEventType.Create("t"), FixedTime, value!));
    }

    [Fact]
    public void Should_reject_default_occurred_at()
    {
        Assert.Throws<ArgumentException>(() =>
            TraceEvent.Create(TraceEventId.Create("e"), "src", TraceEventType.Create("t"), default, "Title"));
    }

    [Fact]
    public void Should_reject_source_system_exceeding_max_length()
    {
        var longValue = new string('x', 129);
        Assert.Throws<ArgumentException>(() =>
            TraceEvent.Create(TraceEventId.Create("e"), longValue, TraceEventType.Create("t"), FixedTime, "Title"));
    }

    [Fact]
    public void Should_reject_display_title_exceeding_max_length()
    {
        var longValue = new string('x', 513);
        Assert.Throws<ArgumentException>(() =>
            TraceEvent.Create(TraceEventId.Create("e"), "src", TraceEventType.Create("t"), FixedTime, longValue));
    }

    [Fact]
    public void Should_trim_source_system()
    {
        var e = TraceEvent.Create(
            TraceEventId.Create("e"), "  github-actions  ",
            TraceEventType.Create("t"), FixedTime, "Title");
        Assert.Equal("github-actions", e.SourceSystem);
    }

    [Fact]
    public void Should_trim_display_title()
    {
        var e = TraceEvent.Create(
            TraceEventId.Create("e"), "src",
            TraceEventType.Create("t"), FixedTime, "  Deploy v2  ");
        Assert.Equal("Deploy v2", e.DisplayTitle);
    }
}
