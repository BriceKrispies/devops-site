using System.Collections.Concurrent;
using DevOpsSite.Application.Audit;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.Telemetry;

/// <summary>
/// In-memory audit adapter for tests and local development.
/// </summary>
public sealed class InMemoryAuditAdapter : IAuditPort
{
    public ConcurrentBag<AuditEvent> Events { get; } = new();

    public Task RecordAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        Events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public void Clear() => Events.Clear();
}
