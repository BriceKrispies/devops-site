using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Contracts.Tests.ServiceHealth;

/// <summary>
/// Port contract test suite for IServiceHealthPort.
/// Constitution §6.2C: Reusable contract suite that any adapter must pass.
/// Any implementation of IServiceHealthPort should execute this suite.
/// </summary>
public abstract class ServiceHealthPortContractTests
{
    protected abstract IServiceHealthPort CreateAdapter();

    /// <summary>
    /// Override to seed a service that will be found by GetHealthAsync.
    /// </summary>
    protected abstract Task SeedKnownService(string serviceId, HealthStatus status, string description);

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "contract-test-001",
        OperationName = "ContractTest",
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    [Fact]
    public async Task Should_return_health_for_known_service()
    {
        await SeedKnownService("contract-svc", HealthStatus.Healthy, "All good");
        var adapter = CreateAdapter();

        var result = await adapter.GetHealthAsync(ServiceId.Create("contract-svc"), Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal("contract-svc", result.Value.ServiceId.Value);
        Assert.Equal(HealthStatus.Healthy, result.Value.Status);
    }

    [Fact]
    public async Task Should_return_not_found_for_unknown_service()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetHealthAsync(ServiceId.Create("unknown-svc"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_result_not_throw_exception()
    {
        var adapter = CreateAdapter();

        // Must return Result, not throw
        var result = await adapter.GetHealthAsync(ServiceId.Create("anything"), Ctx());

        Assert.NotNull(result);
        Assert.True(result.IsSuccess || result.IsFailure);
    }

    [Fact]
    public async Task Error_should_use_standard_error_codes()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetHealthAsync(ServiceId.Create("nonexistent"), Ctx());

        if (result.IsFailure)
        {
            Assert.True(Enum.IsDefined(typeof(ErrorCode), result.Error.Code),
                $"Error code {result.Error.Code} is not in the standard taxonomy.");
        }
    }

    [Fact]
    public async Task Error_should_include_operation_context()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetHealthAsync(ServiceId.Create("nonexistent"), Ctx());

        if (result.IsFailure)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.Error.OperationName));
            Assert.False(string.IsNullOrWhiteSpace(result.Error.CorrelationId));
        }
    }
}
