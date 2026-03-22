using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Domain.Tests.ValueObjects;

/// <summary>
/// Domain specification tests for ServiceId value object.
/// Constitution §6.2A: invariants, business rules, edge cases.
/// </summary>
public sealed class ServiceIdTests
{
    [Fact]
    public void Should_create_valid_service_id()
    {
        var id = ServiceId.Create("my-service");
        Assert.Equal("my-service", id.Value);
    }

    [Fact]
    public void Should_trim_whitespace()
    {
        var id = ServiceId.Create("  my-service  ");
        Assert.Equal("my-service", id.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_empty_value(string? value)
    {
        Assert.Throws<ArgumentException>(() => ServiceId.Create(value!));
    }

    [Fact]
    public void Should_reject_value_exceeding_max_length()
    {
        var longValue = new string('a', 257);
        Assert.Throws<ArgumentException>(() => ServiceId.Create(longValue));
    }

    [Fact]
    public void Should_accept_value_at_max_length()
    {
        var value = new string('a', 256);
        var id = ServiceId.Create(value);
        Assert.Equal(256, id.Value.Length);
    }

    [Fact]
    public void Equal_values_should_be_equal()
    {
        var a = ServiceId.Create("svc-1");
        var b = ServiceId.Create("svc-1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_values_should_not_be_equal()
    {
        var a = ServiceId.Create("svc-1");
        var b = ServiceId.Create("svc-2");
        Assert.NotEqual(a, b);
    }
}
