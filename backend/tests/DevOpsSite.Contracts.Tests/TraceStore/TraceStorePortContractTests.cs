using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Queries;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Contracts.Tests.TraceStore;

/// <summary>
/// Port contract / certification test suite for ITraceStorePort.
/// Constitution §6.2C: Reusable contract suite that any adapter must pass.
/// </summary>
public abstract class TraceStorePortContractTests
{
    protected abstract ITraceStorePort CreateAdapter();

    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "contract-test-ts-001",
        OperationName = "ContractTest",
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    private static TraceEvent MakeEvent(string id = "evt-1", string service = "api-service",
        string eventType = "deployment", DateTimeOffset? occurredAt = null) =>
        TraceEvent.Create(
            TraceEventId.Create(id), "github-actions",
            TraceEventType.Create(eventType),
            occurredAt ?? FixedTime,
            $"Event {id}",
            serviceName: service);

    [Fact]
    public async Task Should_append_and_query_events()
    {
        var adapter = CreateAdapter();
        var events = new List<TraceEvent> { MakeEvent("evt-1"), MakeEvent("evt-2") };

        var appendResult = await adapter.AppendAsync(events, Ctx());
        Assert.True(appendResult.IsSuccess);
        Assert.Equal(2, appendResult.Value);

        var queryResult = await adapter.QueryAsync(new TraceQuery(), Ctx());
        Assert.True(queryResult.IsSuccess);
        Assert.Equal(2, queryResult.Value.Count);
    }

    [Fact]
    public async Task Should_filter_by_service_name()
    {
        var adapter = CreateAdapter();
        var events = new List<TraceEvent>
        {
            MakeEvent("evt-1", service: "api-service"),
            MakeEvent("evt-2", service: "web-service")
        };
        await adapter.AppendAsync(events, Ctx());

        var result = await adapter.QueryAsync(new TraceQuery { ServiceName = "api-service" }, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("api-service", result.Value[0].ServiceName);
    }

    [Fact]
    public async Task Should_filter_by_event_type()
    {
        var adapter = CreateAdapter();
        var events = new List<TraceEvent>
        {
            MakeEvent("evt-1", eventType: "deployment"),
            MakeEvent("evt-2", eventType: "incident")
        };
        await adapter.AppendAsync(events, Ctx());

        var result = await adapter.QueryAsync(new TraceQuery { EventType = "deployment" }, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("deployment", result.Value[0].EventType.Value);
    }

    [Fact]
    public async Task Should_filter_by_time_range()
    {
        var adapter = CreateAdapter();
        var t1 = new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 3, 24, 12, 0, 0, TimeSpan.Zero);
        var events = new List<TraceEvent>
        {
            MakeEvent("evt-1", occurredAt: t1),
            MakeEvent("evt-2", occurredAt: t2),
            MakeEvent("evt-3", occurredAt: t3)
        };
        await adapter.AppendAsync(events, Ctx());

        var result = await adapter.QueryAsync(new TraceQuery
        {
            From = new DateTimeOffset(2026, 3, 21, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 3, 23, 0, 0, 0, TimeSpan.Zero)
        }, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("evt-2", result.Value[0].Id.Value);
    }

    [Fact]
    public async Task Should_respect_limit()
    {
        var adapter = CreateAdapter();
        var events = Enumerable.Range(1, 10)
            .Select(i => MakeEvent($"evt-{i}"))
            .ToList();
        await adapter.AppendAsync(events, Ctx());

        var result = await adapter.QueryAsync(new TraceQuery { Limit = 3 }, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task Should_return_empty_list_when_no_matches()
    {
        var adapter = CreateAdapter();

        var result = await adapter.QueryAsync(new TraceQuery { ServiceName = "nonexistent" }, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Should_order_by_occurred_at_descending()
    {
        var adapter = CreateAdapter();
        var t1 = new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);
        var events = new List<TraceEvent>
        {
            MakeEvent("evt-old", occurredAt: t1),
            MakeEvent("evt-new", occurredAt: t2)
        };
        await adapter.AppendAsync(events, Ctx());

        var result = await adapter.QueryAsync(new TraceQuery(), Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal("evt-new", result.Value[0].Id.Value);
        Assert.Equal("evt-old", result.Value[1].Id.Value);
    }

    [Fact]
    public async Task Should_return_result_not_throw_exception()
    {
        var adapter = CreateAdapter();

        var result = await adapter.QueryAsync(new TraceQuery(), Ctx());

        Assert.NotNull(result);
        Assert.True(result.IsSuccess || result.IsFailure);
    }
}
