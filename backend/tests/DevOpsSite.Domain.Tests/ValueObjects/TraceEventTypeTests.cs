using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.ValueObjects;

public sealed class TraceEventTypeTests
{
    [Fact]
    public void Should_create_with_valid_value()
    {
        var t = TraceEventType.Create("deployment");
        Assert.Equal("deployment", t.Value);
    }

    [Fact]
    public void Should_normalize_to_lowercase()
    {
        var t = TraceEventType.Create("DEPLOYMENT");
        Assert.Equal("deployment", t.Value);
    }

    [Fact]
    public void Should_trim_whitespace()
    {
        var t = TraceEventType.Create("  build  ");
        Assert.Equal("build", t.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_value(string? value)
    {
        Assert.Throws<ArgumentException>(() => TraceEventType.Create(value!));
    }

    [Fact]
    public void Should_reject_value_exceeding_max_length()
    {
        var longValue = new string('x', 129);
        Assert.Throws<ArgumentException>(() => TraceEventType.Create(longValue));
    }

    [Fact]
    public void Should_have_value_equality()
    {
        var a = TraceEventType.Create("incident");
        var b = TraceEventType.Create("INCIDENT");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Should_not_equal_different_value()
    {
        var a = TraceEventType.Create("deployment");
        var b = TraceEventType.Create("incident");
        Assert.NotEqual(a, b);
    }
}
