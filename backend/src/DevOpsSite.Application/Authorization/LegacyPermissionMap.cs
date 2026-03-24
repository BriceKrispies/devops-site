namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Maps legacy 1-based permission numbers (from old DevOps site DynamoDB roles)
/// to the new Permission model. Used during side-by-side transition.
///
/// The old site stores permissions as an array of stringified numbers like ["1","2","3"].
/// Each number corresponds to a feature area on the old site.
/// </summary>
public static class LegacyPermissionMap
{
    private static readonly IReadOnlyDictionary<int, Permission[]> Map = new Dictionary<int, Permission[]>
    {
        // 1: View Process Logs / Home
        [1] = [Permission.WellKnown.ServiceHealthRead, Permission.WellKnown.WorkItemRead],
        // 2: Copy Databases
        [2] = [Permission.WellKnown.DatabasesRead, Permission.WellKnown.DatabasesOperate],
        // 3: Remove Clients
        [3] = [Permission.WellKnown.DatabasesOperate],
        // 4: Clone Database
        [4] = [Permission.WellKnown.DatabasesRead, Permission.WellKnown.DatabasesOperate],
        // 5: Security Logs
        [5] = [Permission.WellKnown.LogsRead],
        // 6: Automate Test Resources
        [6] = [Permission.WellKnown.TraceEventsRead, Permission.WellKnown.TraceEventsWrite],
        // 7: Users/Roles Management
        [7] = [Permission.WellKnown.AdminUsers, Permission.WellKnown.AdminRoles],
        // 8: Queue Status
        [8] = [Permission.WellKnown.QueuesRead],
        // 9: PipeLines
        [9] = [Permission.WellKnown.QueuesRead, Permission.WellKnown.QueuesOperate],
        // 10: Point Loader
        [10] = [Permission.WellKnown.TraceEventsRead, Permission.WellKnown.TraceEventsWrite, Permission.WellKnown.TraceEventsIngest],
        // 11: SQS DLQ Manager
        [11] = [Permission.WellKnown.QueuesRead, Permission.WellKnown.QueuesOperate],
    };

    /// <summary>
    /// Resolves a set of legacy permission numbers to the corresponding new permissions.
    /// Unknown numbers are silently ignored.
    /// </summary>
    public static IReadOnlySet<Permission> Resolve(IEnumerable<int> legacyNumbers)
    {
        var result = new HashSet<Permission>();
        foreach (var num in legacyNumbers)
        {
            if (Map.TryGetValue(num, out var permissions))
            {
                foreach (var p in permissions)
                    result.Add(p);
            }
        }
        return result;
    }
}
