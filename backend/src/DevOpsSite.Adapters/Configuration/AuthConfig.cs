using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Adapters.Configuration;

/// <summary>
/// Authentication configuration. Constitution §11: typed, validated at startup.
/// Controls which authentication mode the host uses.
/// </summary>
public sealed class AuthConfig
{
    /// <summary>
    /// Authentication mode. Must be one of: "DevelopmentBypass", "Oidc".
    /// DevelopmentBypass is ONLY allowed in Development environment.
    /// </summary>
    [Required(ErrorMessage = "Auth:Mode is required.")]
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// Active persona name when Mode is DevelopmentBypass.
    /// Must match a key in the Personas dictionary.
    /// </summary>
    public string ActivePersona { get; set; } = "viewer";
}

/// <summary>
/// Well-known authentication modes. Explicit names that are easy to grep.
/// </summary>
public static class AuthMode
{
    /// <summary>
    /// Development-only bypass. Injects a local persona as the authenticated actor.
    /// MUST NOT be used outside the Development environment.
    /// </summary>
    public const string DevelopmentBypass = "DevelopmentBypass";

    /// <summary>
    /// Real OIDC/Okta integration (future).
    /// </summary>
    public const string Oidc = "Oidc";

    public static bool IsDevelopmentBypass(string mode) =>
        string.Equals(mode, DevelopmentBypass, StringComparison.OrdinalIgnoreCase);
}
