using DevOpsSite.Application.Context;
using DevOpsSite.Application.Queries;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for storing and querying normalized trace events.
/// Constitution §10: External systems consumed through explicit ports.
/// The port speaks domain language — no vendor types, no storage implementation details.
/// </summary>
public interface ITraceStorePort
{
    /// <summary>
    /// Append one or more trace events to the store. Batch write.
    /// </summary>
    Task<Result<int>> AppendAsync(IReadOnlyList<TraceEvent> events, OperationContext ctx, CancellationToken ct = default);

    /// <summary>
    /// Query trace events matching the given filter criteria.
    /// </summary>
    Task<Result<IReadOnlyList<TraceEvent>>> QueryAsync(TraceQuery query, OperationContext ctx, CancellationToken ct = default);
}
