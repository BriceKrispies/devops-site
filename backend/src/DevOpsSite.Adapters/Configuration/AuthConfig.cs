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

    /// <summary>
    /// OIDC Authority URL (e.g., Cognito issuer).
    /// Required when Mode is "Oidc".
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// OIDC Client ID for the application.
    /// Required when Mode is "Oidc".
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Valid audiences for JWT validation.
    /// If not set, defaults to ClientId.
    /// </summary>
    public string[]? ValidAudiences { get; set; }
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
