using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Adapters.Jira;

/// <summary>
/// Typed configuration for Jira adapter. Constitution §11.
/// Validated at startup — fail-fast on invalid values.
/// </summary>
public sealed class JiraConfig
{
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string CredentialsRef { get; set; } = string.Empty;

    [Range(1000, 60000)]
    public int TimeoutMs { get; set; } = 10000;

    [Range(0, 5)]
    public int MaxRetries { get; set; } = 2;
}
