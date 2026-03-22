using DevOpsSite.Application.Context;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for retrieving normalized work item summaries from external issue trackers.
/// Constitution §10: All external systems must be consumed through explicit ports.
/// The port speaks our domain language, not vendor language.
/// </summary>
public interface IWorkItemPort
{
    Task<Result<WorkItemSummary>> GetByKeyAsync(WorkItemKey key, OperationContext ctx, CancellationToken ct = default);
}
