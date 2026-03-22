using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.ValueObjects;

public sealed class TraceEventIdTests
{
    [Fact]
    public void Should_create_with_valid_value()
    {
        var id = TraceEventId.Create("evt-123");
        Assert.Equal("evt-123", id.Value);
    }

    [Fact]
    public void Should_trim_whitespace()
    {
        var id = TraceEventId.Create("  evt-123  ");
        Assert.Equal("evt-123", id.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_value(string? value)
    {
        Assert.Throws<ArgumentException>(() => TraceEventId.Create(value!));
    }

    [Fact]
    public void Should_reject_value_exceeding_max_length()
    {
        var longValue = new string('x', 513);
        Assert.Throws<ArgumentException>(() => TraceEventId.Create(longValue));
    }

    [Fact]
    public void Should_allow_max_length_value()
    {
        var maxValue = new string('x', 512);
        var id = TraceEventId.Create(maxValue);
        Assert.Equal(512, id.Value.Length);
    }

    [Fact]
    public void Should_have_value_equality()
    {
        var a = TraceEventId.Create("evt-1");
        var b = TraceEventId.Create("evt-1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Should_not_equal_different_value()
    {
        var a = TraceEventId.Create("evt-1");
        var b = TraceEventId.Create("evt-2");
        Assert.NotEqual(a, b);
    }
}
