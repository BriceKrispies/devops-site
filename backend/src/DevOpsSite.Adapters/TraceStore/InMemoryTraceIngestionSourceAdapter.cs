using System.Collections.Concurrent;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Adapters.TraceStore;

/// <summary>
/// In-memory trace ingestion source for local development and tests.
/// Events are enqueued via Enqueue() and fetched/acknowledged by the ingestion capability.
/// Thread-safe. Not durable — data is lost on restart.
/// </summary>
public sealed class InMemoryTraceIngestionSourceAdapter : ITraceIngestionSourcePort
{
    private readonly ConcurrentQueue<TraceEventInput> _pending = new();
    private readonly ConcurrentBag<string> _acknowledged = new();

    public Task<Result<IReadOnlyList<TraceEventInput>>> FetchPendingAsync(
        int maxBatchSize, OperationContext ctx, CancellationToken ct = default)
    {
        var batch = new List<TraceEventInput>();
        while (batch.Count < maxBatchSize && _pending.TryDequeue(out var item))
        {
            batch.Add(item);
        }
        return Task.FromResult(Result<IReadOnlyList<TraceEventInput>>.Success(batch));
    }

    public Task AcknowledgeAsync(IReadOnlyList<string> eventIds, OperationContext ctx, CancellationToken ct = default)
    {
        foreach (var id in eventIds)
            _acknowledged.Add(id);
        return Task.CompletedTask;
    }

    /// <summary>For test setup and local development — enqueue events for ingestion.</summary>
    public void Enqueue(TraceEventInput input) => _pending.Enqueue(input);

    /// <summary>For test assertions — get all acknowledged event IDs.</summary>
    public IReadOnlyList<string> GetAcknowledged() => _acknowledged.ToList();

    /// <summary>For test assertions — get count of pending events.</summary>
    public int PendingCount => _pending.Count;
}
