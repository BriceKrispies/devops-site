namespace DevOpsSite.Application.Authorization;

/// <summary>
/// A user resolved from the external user/role store with permissions
/// already mapped to the internal permission model.
/// </summary>
public sealed record ResolvedUser
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public required string RoleId { get; init; }
    public required string RoleName { get; init; }
    public required IReadOnlySet<Permission> Permissions { get; init; }
}
