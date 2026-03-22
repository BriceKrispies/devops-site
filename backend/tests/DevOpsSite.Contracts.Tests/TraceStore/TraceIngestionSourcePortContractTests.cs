using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Contracts.Tests.TraceStore;

/// <summary>
/// Port contract / certification test suite for ITraceIngestionSourcePort.
/// Constitution §6.2C: Reusable contract suite that any adapter must pass.
/// </summary>
public abstract class TraceIngestionSourcePortContractTests
{
    protected abstract ITraceIngestionSourcePort CreateAdapter();

    protected abstract Task SeedPendingEvent(string id, string sourceSystem, string eventType, string title);

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "contract-test-tis-001",
        OperationName = "ContractTest",
        Timestamp = FixedTime,
        Source = OperationSource.BackgroundJob
    };

    [Fact]
    public async Task Should_return_empty_list_when_nothing_pending()
    {
        var adapter = CreateAdapter();

        var result = await adapter.FetchPendingAsync(10, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Should_return_seeded_events()
    {
        await SeedPendingEvent("evt-1", "ci", "build", "Build #1");
        var adapter = CreateAdapter();

        var result = await adapter.FetchPendingAsync(10, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("evt-1", result.Value[0].Id);
    }

    [Fact]
    public async Task Should_respect_max_batch_size()
    {
        await SeedPendingEvent("evt-1", "ci", "build", "Build #1");
        await SeedPendingEvent("evt-2", "ci", "build", "Build #2");
        await SeedPendingEvent("evt-3", "ci", "build", "Build #3");
        var adapter = CreateAdapter();

        var result = await adapter.FetchPendingAsync(2, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Should_not_return_already_fetched_events()
    {
        await SeedPendingEvent("evt-1", "ci", "build", "Build #1");
        var adapter = CreateAdapter();

        // First fetch
        var result1 = await adapter.FetchPendingAsync(10, Ctx());
        Assert.Single(result1.Value);

        // Second fetch should be empty (events consumed)
        var result2 = await adapter.FetchPendingAsync(10, Ctx());
        Assert.Empty(result2.Value);
    }

    [Fact]
    public async Task Should_accept_acknowledge_without_error()
    {
        await SeedPendingEvent("evt-1", "ci", "build", "Build #1");
        var adapter = CreateAdapter();

        await adapter.FetchPendingAsync(10, Ctx());

        // Should not throw
        await adapter.AcknowledgeAsync(new[] { "evt-1" }, Ctx());
    }

    [Fact]
    public async Task Should_return_result_not_throw_exception()
    {
        var adapter = CreateAdapter();

        var result = await adapter.FetchPendingAsync(10, Ctx());

        Assert.NotNull(result);
        Assert.True(result.IsSuccess || result.IsFailure);
    }
}
