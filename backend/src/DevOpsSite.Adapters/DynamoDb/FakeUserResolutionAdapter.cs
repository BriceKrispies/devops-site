using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.DynamoDb;

/// <summary>
/// Fake adapter for tests and local development. Returns configurable user data.
/// </summary>
public sealed class FakeUserResolutionAdapter : IUserResolutionPort
{
    private readonly Dictionary<string, ResolvedUser> _data = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(ResolvedUser user) => _data[user.Username] = user;

    public Task<ResolvedUser?> ResolveByEmailAsync(string email, CancellationToken ct = default)
    {
        _data.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }
}
