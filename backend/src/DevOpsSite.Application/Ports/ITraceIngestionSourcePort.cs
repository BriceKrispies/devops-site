using DevOpsSite.Application.Context;
using DevOpsSite.Application.Results;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for fetching pending trace events from an external ingestion source.
/// Constitution §10: External systems consumed through explicit ports.
/// The port speaks domain language — implementations may poll a queue, file system, webhook buffer, etc.
/// </summary>
public interface ITraceIngestionSourcePort
{
    /// <summary>
    /// Fetch the next batch of pending trace event inputs from the source.
    /// Returns an empty list when no events are pending.
    /// </summary>
    Task<Result<IReadOnlyList<TraceEventInput>>> FetchPendingAsync(int maxBatchSize, OperationContext ctx, CancellationToken ct = default);

    /// <summary>
    /// Acknowledge that a batch of events has been successfully processed.
    /// Implementations may use this to remove items from a queue or mark them as consumed.
    /// </summary>
    Task AcknowledgeAsync(IReadOnlyList<string> eventIds, OperationContext ctx, CancellationToken ct = default);
}
