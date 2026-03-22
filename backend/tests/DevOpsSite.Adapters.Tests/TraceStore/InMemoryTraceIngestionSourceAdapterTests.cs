using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Contracts.Tests.TraceStore;

namespace DevOpsSite.Adapters.Tests.TraceStore;

/// <summary>
/// Adapter test for InMemoryTraceIngestionSourceAdapter.
/// Constitution §6.2D: Adapter tested against contract suite.
/// </summary>
public sealed class InMemoryTraceIngestionSourceAdapterTests : TraceIngestionSourcePortContractTests
{
    private readonly InMemoryTraceIngestionSourceAdapter _adapter = new();

    protected override ITraceIngestionSourcePort CreateAdapter() => _adapter;

    protected override Task SeedPendingEvent(string id, string sourceSystem, string eventType, string title)
    {
        _adapter.Enqueue(new TraceEventInput
        {
            Id = id,
            SourceSystem = sourceSystem,
            EventType = eventType,
            OccurredAt = new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero),
            DisplayTitle = title
        });
        return Task.CompletedTask;
    }
}
