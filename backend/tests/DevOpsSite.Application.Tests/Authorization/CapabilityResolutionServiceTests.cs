using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Adapters.Capabilities;

namespace DevOpsSite.Application.Tests.Authorization;

/// <summary>
/// Tests for the capability resolution service.
/// Verifies the resolution order: kill switch → implementation status → auth/role → override → default.
/// </summary>
public sealed class CapabilityResolutionServiceTests
{
    private readonly InMemoryCapabilityOverrideStore _store = new();
    private readonly CapabilityResolutionService _service;

    private readonly OperationContext _adminCtx;
    private readonly OperationContext _viewerCtx;
    private readonly OperationContext _anonymousCtx;

    public CapabilityResolutionServiceTests()
    {
        _service = new CapabilityResolutionService(_store);

        _adminCtx = new OperationContext
        {
            CorrelationId = "test-admin",
            OperationName = "ResolveCapabilities",
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = new ActorIdentity { Id = "admin", Type = ActorType.User },
            Permissions = new HashSet<Permission>
            {
                Permission.WellKnown.ServiceHealthRead,
                Permission.WellKnown.WorkItemRead,
                Permission.WellKnown.TraceEventsRead,
                Permission.WellKnown.TraceEventsWrite,
                Permission.WellKnown.TraceEventsIngest,
                Permission.WellKnown.QueuesRead,
                Permission.WellKnown.QueuesOperate,
                Permission.WellKnown.DatabasesRead,
                Permission.WellKnown.DatabasesOperate,
                Permission.WellKnown.LogsRead
            }
        };

        _viewerCtx = new OperationContext
        {
            CorrelationId = "test-viewer",
            OperationName = "ResolveCapabilities",
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = new ActorIdentity { Id = "viewer", Type = ActorType.User },
            Permissions = new HashSet<Permission>
            {
                Permission.WellKnown.ServiceHealthRead,
                Permission.WellKnown.WorkItemRead,
                Permission.WellKnown.TraceEventsRead
            }
        };

        _anonymousCtx = new OperationContext
        {
            CorrelationId = "test-anon",
            OperationName = "ResolveCapabilities",
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest
        };
    }

    // ──────────────────────────────────────────────────────────────
    //  Default resolution
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Ready_capability_with_permissions_resolves_to_enabled()
    {
        var result = _service.Resolve("GetServiceHealth", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Enabled, result.Status);
        Assert.Null(result.Message);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void ResolveAll_returns_all_catalog_capabilities()
    {
        var results = _service.ResolveAll(_adminCtx);

        Assert.Equal(OperationalCapabilityCatalog.All.Count, results.Count);
    }

    [Fact]
    public void Unknown_capability_returns_null()
    {
        var result = _service.Resolve("NonExistent", _adminCtx);

        Assert.Null(result);
    }

    // ──────────────────────────────────────────────────────────────
    //  Kill switch — highest priority
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Active_kill_switch_overrides_everything()
    {
        _store.SetKillSwitch(new KillSwitch
        {
            OperationName = "GetServiceHealth",
            IsActive = true,
            Reason = "Emergency shutdown"
        });

        var result = _service.Resolve("GetServiceHealth", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Disabled, result.Status);
        Assert.Equal("kill_switch", result.Reason);
        Assert.Equal("Emergency shutdown", result.Message);
    }

    [Fact]
    public void Inactive_kill_switch_does_not_affect_resolution()
    {
        _store.SetKillSwitch(new KillSwitch
        {
            OperationName = "GetServiceHealth",
            IsActive = false,
            Reason = "Previously disabled"
        });

        var result = _service.Resolve("GetServiceHealth", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Enabled, result.Status);
    }

    [Fact]
    public void IsKillSwitched_returns_true_for_active_kill_switch()
    {
        _store.SetKillSwitch(new KillSwitch
        {
            OperationName = "GetServiceHealth",
            IsActive = true,
            Reason = "Down"
        });

        Assert.True(_service.IsKillSwitched("GetServiceHealth"));
        Assert.False(_service.IsKillSwitched("GetWorkItem"));
    }

    // ──────────────────────────────────────────────────────────────
    //  Implementation status
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Planned_capability_resolves_to_disabled_with_not_implemented()
    {
        var result = _service.Resolve("QueuesRead", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Disabled, result.Status);
        Assert.Equal("not_implemented", result.Reason);
        Assert.Null(result.Route);
    }

    [Fact]
    public void Kill_switch_takes_priority_over_planned_status()
    {
        _store.SetKillSwitch(new KillSwitch
        {
            OperationName = "QueuesRead",
            IsActive = true,
            Reason = "Kill switched"
        });

        var result = _service.Resolve("QueuesRead", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Disabled, result.Status);
        Assert.Equal("kill_switch", result.Reason);
    }

    // ──────────────────────────────────────────────────────────────
    //  Auth/role checks
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Anonymous_user_sees_authenticated_capabilities_as_hidden()
    {
        var result = _service.Resolve("GetServiceHealth", _anonymousCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Hidden, result.Status);
        Assert.Equal("unauthenticated", result.Reason);
    }

    [Fact]
    public void Viewer_without_write_permission_sees_write_capabilities_as_hidden()
    {
        // Viewer has traceevents:read but not traceevents:write
        var result = _service.Resolve("AddTraceEvents", _viewerCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Hidden, result.Status);
        Assert.Equal("forbidden", result.Reason);
    }

    [Fact]
    public void Viewer_with_read_permission_sees_read_capability_as_enabled()
    {
        var result = _service.Resolve("GetServiceHealth", _viewerCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Enabled, result.Status);
    }

    // ──────────────────────────────────────────────────────────────
    //  Explicit overrides
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Override_changes_status_for_enabled_capability()
    {
        _store.SetOverride(new CapabilityOverride
        {
            OperationName = "GetServiceHealth",
            Status = ResolvedCapabilityStatus.ReadOnly,
            Reason = "Maintenance mode"
        });

        var result = _service.Resolve("GetServiceHealth", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.ReadOnly, result.Status);
        Assert.Equal("override", result.Reason);
        Assert.Equal("Maintenance mode", result.Message);
    }

    [Fact]
    public void Kill_switch_takes_priority_over_override()
    {
        _store.SetKillSwitch(new KillSwitch
        {
            OperationName = "GetServiceHealth",
            IsActive = true,
            Reason = "Emergency"
        });
        _store.SetOverride(new CapabilityOverride
        {
            OperationName = "GetServiceHealth",
            Status = ResolvedCapabilityStatus.Enabled,
            Reason = "Should be ignored"
        });

        var result = _service.Resolve("GetServiceHealth", _adminCtx);

        Assert.NotNull(result);
        Assert.Equal(ResolvedCapabilityStatus.Disabled, result.Status);
        Assert.Equal("kill_switch", result.Reason);
    }

    [Fact]
    public void Override_does_not_apply_to_planned_capabilities()
    {
        _store.SetOverride(new CapabilityOverride
        {
            OperationName = "QueuesRead",
            Status = ResolvedCapabilityStatus.Enabled,
            Reason = "Force enable"
        });

        var result = _service.Resolve("QueuesRead", _adminCtx);

        Assert.NotNull(result);
        // Planned status takes priority over overrides
        Assert.Equal(ResolvedCapabilityStatus.Disabled, result.Status);
        Assert.Equal("not_implemented", result.Reason);
    }

    // ──────────────────────────────────────────────────────────────
    //  Metadata and contract shape
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Resolved_capability_includes_metadata()
    {
        var result = _service.Resolve("GetServiceHealth", _adminCtx);

        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal("ServiceHealth", result.Metadata.Category);
        Assert.False(result.Metadata.Privileged);
        Assert.Equal("synchronous", result.Metadata.ExecutionMode);
    }

    [Fact]
    public void Resolved_capability_includes_area()
    {
        var result = _service.Resolve("GetServiceHealth", _adminCtx);
        Assert.Equal("overview", result!.Area);

        var traceResult = _service.Resolve("QueryTraceEvents", _adminCtx);
        Assert.Equal("investigate", traceResult!.Area);

        var queuesResult = _service.Resolve("QueuesRead", _adminCtx);
        Assert.Equal("queues", queuesResult!.Area);
    }

    [Fact]
    public void Resolved_capability_includes_risk()
    {
        var low = _service.Resolve("GetServiceHealth", _adminCtx);
        Assert.Equal("low", low!.Risk);

        var high = _service.Resolve("QueuesRedriveDlq", _adminCtx);
        Assert.Equal("high", high!.Risk);

        var critical = _service.Resolve("DatabasesCloneNonProd", _adminCtx);
        Assert.Equal("critical", critical!.Risk);
    }

    [Fact]
    public void Resolved_capability_includes_permissions()
    {
        var result = _service.Resolve("GetServiceHealth", _adminCtx);
        Assert.Contains("servicehealth:read", result!.Permissions);
    }
}
