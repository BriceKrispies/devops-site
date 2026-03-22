using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;

namespace DevOpsSite.Host.Authentication;

/// <summary>
/// Local development personas. Each maps to the backend's internal permission model.
/// These are explicit, inspectable, and used ONLY in DevelopmentBypass auth mode.
/// </summary>
public static class DevPersonas
{
    public static readonly IReadOnlyDictionary<string, DevPersona> All = new Dictionary<string, DevPersona>(StringComparer.OrdinalIgnoreCase)
    {
        ["viewer"] = new DevPersona
        {
            Id = "dev:viewer",
            DisplayName = "Dev Viewer",
            Permissions = new HashSet<Permission>
            {
                Permission.WellKnown.ServiceHealthRead,
                Permission.WellKnown.WorkItemRead,
                Permission.WellKnown.TraceEventsRead
            }
        },
        ["operator"] = new DevPersona
        {
            Id = "dev:operator",
            DisplayName = "Dev Operator",
            Permissions = new HashSet<Permission>
            {
                Permission.WellKnown.ServiceHealthRead,
                Permission.WellKnown.WorkItemRead,
                Permission.WellKnown.TraceEventsRead,
                Permission.WellKnown.TraceEventsWrite,
                Permission.WellKnown.TraceEventsIngest
            }
        },
        ["admin"] = new DevPersona
        {
            Id = "dev:admin",
            DisplayName = "Dev Admin",
            Permissions = new HashSet<Permission>
            {
                // Existing
                Permission.WellKnown.ServiceHealthRead,
                Permission.WellKnown.WorkItemRead,
                Permission.WellKnown.TraceEventsRead,
                Permission.WellKnown.TraceEventsWrite,
                Permission.WellKnown.TraceEventsIngest,
                // Future AWS operational capabilities
                Permission.WellKnown.QueuesRead,
                Permission.WellKnown.QueuesOperate,
                Permission.WellKnown.DatabasesRead,
                Permission.WellKnown.DatabasesOperate,
                Permission.WellKnown.LogsRead
            }
        }
    };

    public static DevPersona GetPersona(string name)
    {
        if (All.TryGetValue(name, out var persona))
            return persona;

        throw new InvalidOperationException(
            $"Unknown dev persona '{name}'. Available: {string.Join(", ", All.Keys)}");
    }
}

public sealed class DevPersona
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlySet<Permission> Permissions { get; init; }

    public ActorIdentity ToActor() => new()
    {
        Id = Id,
        Type = ActorType.User,
        DisplayName = DisplayName
    };
}
