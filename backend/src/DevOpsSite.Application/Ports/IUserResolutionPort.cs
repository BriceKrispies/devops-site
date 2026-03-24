using DevOpsSite.Application.Authorization;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for resolving a user's identity and permissions from an external user store.
/// Constitution §10: Vendor-specific storage is abstracted behind this port.
/// </summary>
public interface IUserResolutionPort
{
    /// <summary>
    /// Resolve a user by email address.
    /// Returns null if the user is not found in the store.
    /// </summary>
    Task<ResolvedUser?> ResolveByEmailAsync(string email, CancellationToken ct = default);
}
