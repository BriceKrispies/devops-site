using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Ports;
using DevOpsSite.Contracts.Tests.TraceStore;

namespace DevOpsSite.Adapters.Tests.TraceStore;

/// <summary>
/// Adapter test for InMemoryTraceStoreAdapter.
/// Constitution §6.2D: Adapter tested against contract suite.
/// </summary>
public sealed class InMemoryTraceStoreAdapterTests : TraceStorePortContractTests
{
    protected override ITraceStorePort CreateAdapter() => new InMemoryTraceStoreAdapter();
}
