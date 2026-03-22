using System.Collections.Concurrent;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Queries;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;

namespace DevOpsSite.Adapters.TraceStore;

/// <summary>
/// In-memory trace store for local development and tests.
/// Thread-safe. Not durable — data is lost on restart.
/// </summary>
public sealed class InMemoryTraceStoreAdapter : ITraceStorePort
{
    private readonly ConcurrentBag<TraceEvent> _events = new();

    public Task<Result<int>> AppendAsync(IReadOnlyList<TraceEvent> events, OperationContext ctx, CancellationToken ct = default)
    {
        foreach (var e in events)
            _events.Add(e);

        return Task.FromResult(Result<int>.Success(events.Count));
    }

    public Task<Result<IReadOnlyList<TraceEvent>>> QueryAsync(TraceQuery query, OperationContext ctx, CancellationToken ct = default)
    {
        IEnumerable<TraceEvent> results = _events;

        if (!string.IsNullOrWhiteSpace(query.ServiceName))
            results = results.Where(e =>
                string.Equals(e.ServiceName, query.ServiceName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.EventType))
            results = results.Where(e =>
                string.Equals(e.EventType.Value, query.EventType, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.SourceSystem))
            results = results.Where(e =>
                string.Equals(e.SourceSystem, query.SourceSystem, StringComparison.OrdinalIgnoreCase));

        if (query.From.HasValue)
            results = results.Where(e => e.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            results = results.Where(e => e.OccurredAt <= query.To.Value);

        var list = results
            .OrderByDescending(e => e.OccurredAt)
            .Take(query.Limit)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<TraceEvent>>.Success(list));
    }

    /// <summary>For test setup — seed events directly.</summary>
    public void Seed(TraceEvent traceEvent) => _events.Add(traceEvent);

    /// <summary>For test assertions — get all stored events.</summary>
    public IReadOnlyList<TraceEvent> GetAll() => _events.ToList();

    /// <summary>For test cleanup.</summary>
    public void Clear() => _events.Clear();
}
