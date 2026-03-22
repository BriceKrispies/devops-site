using DevOpsSite.Application.Audit;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for emitting audit events. Constitution §7.5.
/// </summary>
public interface IAuditPort
{
    Task RecordAsync(AuditEvent auditEvent, CancellationToken ct = default);
}
