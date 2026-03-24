using DevOpsSite.Adapters.DynamoDb;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;
// ResolvedUser is in Application.Authorization namespace
using DevOpsSite.Contracts.Tests.UserResolution;

namespace DevOpsSite.Adapters.Tests.DynamoDb;

/// <summary>
/// Runs the port contract suite against the FakeUserResolutionAdapter.
/// Constitution §6.2C: Every adapter must pass the contract suite.
/// </summary>
public sealed class FakeUserResolutionAdapterTests : UserResolutionPortContractTests
{
    private readonly FakeUserResolutionAdapter _adapter = new();

    protected override IUserResolutionPort CreateAdapter() => _adapter;

    protected override Task SeedUser(string email, string userId, string roleId, string roleName, IReadOnlySet<Permission> permissions)
    {
        _adapter.Seed(new ResolvedUser
        {
            UserId = userId,
            Username = email,
            RoleId = roleId,
            RoleName = roleName,
            Permissions = permissions
        });
        return Task.CompletedTask;
    }
}
