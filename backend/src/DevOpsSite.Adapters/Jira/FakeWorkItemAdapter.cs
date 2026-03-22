using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.Jira;

/// <summary>
/// Fake adapter for tests and local development. Returns configurable work item data.
/// </summary>
public sealed class FakeWorkItemAdapter : IWorkItemPort
{
    private readonly Dictionary<string, WorkItemSummary> _data = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(WorkItemSummary item) => _data[item.Key.Value] = item;

    public Task<Result<WorkItemSummary>> GetByKeyAsync(WorkItemKey key, OperationContext ctx, CancellationToken ct = default)
    {
        if (_data.TryGetValue(key.Value, out var item))
            return Task.FromResult(Result<WorkItemSummary>.Success(item));

        return Task.FromResult(Result<WorkItemSummary>.Failure(
            AppError.NotFound(
                $"Work item '{key.Value}' not found.",
                ctx.OperationName,
                ctx.CorrelationId)));
    }
}
