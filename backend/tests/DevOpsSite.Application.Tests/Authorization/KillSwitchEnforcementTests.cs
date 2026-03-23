using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Adapters.Capabilities;

namespace DevOpsSite.Application.Tests.Authorization;

/// <summary>
/// Tests that kill switches are enforced by the AuthorizationService.
/// Kill switches block execution regardless of user permissions.
/// </summary>
public sealed class KillSwitchEnforcementTests
{
    [Fact]
    public void Kill_switch_blocks_authorized_user()
    {
        var store = new InMemoryCapabilityOverrideStore();
        store.SetKillSwitch(new KillSwitch
        {
            OperationName = GetServiceHealthHandler.OperationName,
            IsActive = true,
            Reason = "Emergency maintenance"
        });

        var registry = new CapabilityRegistry();
        registry.Register(GetServiceHealthHandler.Descriptor);
        var authz = new AuthorizationService(registry, store);

        var ctx = new OperationContext
        {
            CorrelationId = "test",
            OperationName = GetServiceHealthHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = new ActorIdentity { Id = "admin", Type = ActorType.User },
            Permissions = new HashSet<Permission> { Permission.WellKnown.ServiceHealthRead }
        };

        var result = authz.Evaluate(GetServiceHealthHandler.OperationName, ctx);

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.KillSwitchActive, result.FailureReason);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Inactive_kill_switch_does_not_block()
    {
        var store = new InMemoryCapabilityOverrideStore();
        store.SetKillSwitch(new KillSwitch
        {
            OperationName = GetServiceHealthHandler.OperationName,
            IsActive = false
        });

        var registry = new CapabilityRegistry();
        registry.Register(GetServiceHealthHandler.Descriptor);
        var authz = new AuthorizationService(registry, store);

        var ctx = new OperationContext
        {
            CorrelationId = "test",
            OperationName = GetServiceHealthHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = new ActorIdentity { Id = "admin", Type = ActorType.User },
            Permissions = new HashSet<Permission> { Permission.WellKnown.ServiceHealthRead }
        };

        var result = authz.Evaluate(GetServiceHealthHandler.OperationName, ctx);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Kill_switch_takes_priority_over_missing_descriptor()
    {
        var store = new InMemoryCapabilityOverrideStore();
        store.SetKillSwitch(new KillSwitch
        {
            OperationName = "SomeOperation",
            IsActive = true,
            Reason = "Killed"
        });

        var registry = new CapabilityRegistry();
        // Do NOT register the descriptor
        var authz = new AuthorizationService(registry, store);

        var ctx = new OperationContext
        {
            CorrelationId = "test",
            OperationName = "SomeOperation",
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest
        };

        var result = authz.Evaluate("SomeOperation", ctx);

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.KillSwitchActive, result.FailureReason);
    }

    [Fact]
    public void AuthorizationService_without_override_store_works_as_before()
    {
        var registry = new CapabilityRegistry();
        registry.Register(GetServiceHealthHandler.Descriptor);
        // Use the single-arg constructor (no override store)
        var authz = new AuthorizationService(registry);

        var ctx = new OperationContext
        {
            CorrelationId = "test",
            OperationName = GetServiceHealthHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = new ActorIdentity { Id = "admin", Type = ActorType.User },
            Permissions = new HashSet<Permission> { Permission.WellKnown.ServiceHealthRead }
        };

        var result = authz.Evaluate(GetServiceHealthHandler.OperationName, ctx);

        Assert.True(result.IsAllowed);
    }
}
