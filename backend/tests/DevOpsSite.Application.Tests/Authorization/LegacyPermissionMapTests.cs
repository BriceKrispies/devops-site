using DevOpsSite.Application.Authorization;

namespace DevOpsSite.Application.Tests.Authorization;

public sealed class LegacyPermissionMapTests
{
    [Theory]
    [InlineData(1, new[] { "servicehealth:read", "workitem:read" })]
    [InlineData(2, new[] { "databases:read", "databases:operate" })]
    [InlineData(3, new[] { "databases:operate" })]
    [InlineData(4, new[] { "databases:read", "databases:operate" })]
    [InlineData(5, new[] { "logs:read" })]
    [InlineData(6, new[] { "traceevents:read", "traceevents:write" })]
    [InlineData(7, new[] { "admin:users", "admin:roles" })]
    [InlineData(8, new[] { "queues:read" })]
    [InlineData(9, new[] { "queues:read", "queues:operate" })]
    [InlineData(10, new[] { "traceevents:read", "traceevents:write", "traceevents:ingest" })]
    [InlineData(11, new[] { "queues:read", "queues:operate" })]
    public void Resolve_SingleNumber_MapsCorrectly(int legacyNumber, string[] expectedValues)
    {
        var result = LegacyPermissionMap.Resolve([legacyNumber]);

        var resultValues = result.Select(p => p.Value).ToHashSet();
        Assert.Equal(expectedValues.ToHashSet(), resultValues);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12)]
    [InlineData(99)]
    public void Resolve_UnknownNumber_ReturnsEmpty(int unknownNumber)
    {
        var result = LegacyPermissionMap.Resolve([unknownNumber]);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_EmptyInput_ReturnsEmpty()
    {
        var result = LegacyPermissionMap.Resolve([]);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_OverlappingNumbers_DeduplicatesPermissions()
    {
        // Numbers 2 and 4 both include databases:read and databases:operate
        var result = LegacyPermissionMap.Resolve([2, 4]);

        Assert.Equal(2, result.Count); // databases:read, databases:operate — not 4
        Assert.Contains(Permission.WellKnown.DatabasesRead, result);
        Assert.Contains(Permission.WellKnown.DatabasesOperate, result);
    }

    [Fact]
    public void Resolve_MixedKnownAndUnknown_IgnoresUnknown()
    {
        var result = LegacyPermissionMap.Resolve([1, 99, 5]);

        Assert.Contains(Permission.WellKnown.ServiceHealthRead, result);
        Assert.Contains(Permission.WellKnown.WorkItemRead, result);
        Assert.Contains(Permission.WellKnown.LogsRead, result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Resolve_AllNumbers_CoverAllWellKnownPermissions()
    {
        var result = LegacyPermissionMap.Resolve(Enumerable.Range(1, 11));

        // Every well-known permission should be represented
        Assert.Contains(Permission.WellKnown.ServiceHealthRead, result);
        Assert.Contains(Permission.WellKnown.WorkItemRead, result);
        Assert.Contains(Permission.WellKnown.DatabasesRead, result);
        Assert.Contains(Permission.WellKnown.DatabasesOperate, result);
        Assert.Contains(Permission.WellKnown.LogsRead, result);
        Assert.Contains(Permission.WellKnown.TraceEventsRead, result);
        Assert.Contains(Permission.WellKnown.TraceEventsWrite, result);
        Assert.Contains(Permission.WellKnown.TraceEventsIngest, result);
        Assert.Contains(Permission.WellKnown.AdminUsers, result);
        Assert.Contains(Permission.WellKnown.AdminRoles, result);
        Assert.Contains(Permission.WellKnown.QueuesRead, result);
        Assert.Contains(Permission.WellKnown.QueuesOperate, result);
    }
}
