using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Contracts.Tests.WorkItem;

/// <summary>
/// Port contract / certification test suite for IWorkItemPort.
/// Constitution §6.2C: Reusable contract suite that any adapter must pass.
/// Any implementation of IWorkItemPort should execute this suite.
/// </summary>
public abstract class WorkItemPortContractTests
{
    protected abstract IWorkItemPort CreateAdapter();

    protected abstract Task SeedKnownWorkItem(string key, string title, string status, string provider);

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "contract-test-wi-001",
        OperationName = "ContractTest",
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    [Fact]
    public async Task Should_return_work_item_for_known_key()
    {
        await SeedKnownWorkItem("PROJ-100", "Fix bug", "Open", "test");
        var adapter = CreateAdapter();

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal("PROJ-100", result.Value.Key.Value);
        Assert.Equal("Fix bug", result.Value.Title);
    }

    [Fact]
    public async Task Should_return_not_found_for_unknown_key()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("UNKNOWN-999"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task Should_return_result_not_throw_exception()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("ANYTHING"), Ctx());

        Assert.NotNull(result);
        Assert.True(result.IsSuccess || result.IsFailure);
    }

    [Fact]
    public async Task Error_should_use_standard_error_codes()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("NONEXISTENT"), Ctx());

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

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("NONEXISTENT"), Ctx());

        if (result.IsFailure)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.Error.OperationName));
            Assert.False(string.IsNullOrWhiteSpace(result.Error.CorrelationId));
        }
    }
}
