using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.ValueObjects;

/// <summary>
/// Domain specification tests for WorkItemKey value object.
/// Constitution §6.2A: invariants, business rules, edge cases.
/// </summary>
public sealed class WorkItemKeyTests
{
    [Fact]
    public void Should_create_valid_key()
    {
        var key = WorkItemKey.Create("PROJ-123");
        Assert.Equal("PROJ-123", key.Value);
    }

    [Fact]
    public void Should_trim_whitespace()
    {
        var key = WorkItemKey.Create("  PROJ-456  ");
        Assert.Equal("PROJ-456", key.Value);
    }

    [Fact]
    public void Should_normalize_to_upper_case()
    {
        var key = WorkItemKey.Create("proj-789");
        Assert.Equal("PROJ-789", key.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_value(string? value)
    {
        Assert.Throws<ArgumentException>(() => WorkItemKey.Create(value!));
    }

    [Fact]
    public void Should_reject_value_exceeding_max_length()
    {
        var longValue = new string('A', 129);
        Assert.Throws<ArgumentException>(() => WorkItemKey.Create(longValue));
    }

    [Fact]
    public void Should_accept_value_at_max_length()
    {
        var value = new string('A', 128);
        var key = WorkItemKey.Create(value);
        Assert.Equal(128, key.Value.Length);
    }

    [Fact]
    public void Equal_values_should_be_equal()
    {
        var a = WorkItemKey.Create("PROJ-1");
        var b = WorkItemKey.Create("PROJ-1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_values_should_not_be_equal()
    {
        var a = WorkItemKey.Create("PROJ-1");
        var b = WorkItemKey.Create("PROJ-2");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Case_variants_should_be_equal_after_normalization()
    {
        var a = WorkItemKey.Create("Proj-10");
        var b = WorkItemKey.Create("PROJ-10");
        Assert.Equal(a, b);
    }
}
