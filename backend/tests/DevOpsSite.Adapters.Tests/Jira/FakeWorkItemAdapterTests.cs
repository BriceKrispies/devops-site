using DevOpsSite.Adapters.Jira;
using DevOpsSite.Application.Ports;
using DevOpsSite.Contracts.Tests.WorkItem;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.Tests.Jira;

/// <summary>
/// Adapter test for FakeWorkItemAdapter.
/// Constitution §6.2D: Adapter tested against contract suite.
/// </summary>
public sealed class FakeWorkItemAdapterTests : WorkItemPortContractTests
{
    private readonly FakeWorkItemAdapter _adapter = new();

    protected override IWorkItemPort CreateAdapter() => _adapter;

    protected override Task SeedKnownWorkItem(string key, string title, string status, string provider)
    {
        var item = WorkItemSummary.Create(
            WorkItemKey.Create(key), title, status,
            null, null, null, provider,
            new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));
        _adapter.Seed(item);
        return Task.CompletedTask;
    }
}
