using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Contracts.Tests.UserResolution;

/// <summary>
/// Port contract / certification test suite for IUserResolutionPort.
/// Constitution §6.2C: Reusable contract suite that any adapter must pass.
/// </summary>
public abstract class UserResolutionPortContractTests
{
    protected abstract IUserResolutionPort CreateAdapter();

    protected abstract Task SeedUser(string email, string userId, string roleId, string roleName, IReadOnlySet<Permission> permissions);

    [Fact]
    public async Task Should_resolve_user_by_email()
    {
        var permissions = new HashSet<Permission>
        {
            Permission.WellKnown.ServiceHealthRead,
            Permission.WellKnown.WorkItemRead
        };
        await SeedUser("test@example.com", "user-1", "role-1", "Viewer", permissions);
        var adapter = CreateAdapter();

        var result = await adapter.ResolveByEmailAsync("test@example.com");

        Assert.NotNull(result);
        Assert.Equal("user-1", result.UserId);
        Assert.Equal("test@example.com", result.Username);
        Assert.Equal("role-1", result.RoleId);
        Assert.Equal("Viewer", result.RoleName);
        Assert.Equal(permissions, result.Permissions);
    }

    [Fact]
    public async Task Should_return_null_for_unknown_email()
    {
        var adapter = CreateAdapter();

        var result = await adapter.ResolveByEmailAsync("unknown@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_be_case_insensitive_on_email()
    {
        var permissions = new HashSet<Permission> { Permission.WellKnown.LogsRead };
        await SeedUser("User@Example.COM", "user-2", "role-2", "Admin", permissions);
        var adapter = CreateAdapter();

        var result = await adapter.ResolveByEmailAsync("user@example.com");

        Assert.NotNull(result);
        Assert.Equal("user-2", result.UserId);
    }

    [Fact]
    public async Task Should_not_throw_on_resolve()
    {
        var adapter = CreateAdapter();

        var result = await adapter.ResolveByEmailAsync("anything@example.com");

        // Should return null, not throw
        Assert.Null(result);
    }
}
