using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Adapters.Configuration;

/// <summary>
/// Typed configuration for the service health adapter.
/// Constitution §11: Configuration must be typed and validated at startup.
/// </summary>
public sealed class ServiceHealthConfig
{
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(1000, 60000)]
    public int TimeoutMs { get; set; } = 5000;

    [Range(0, 5)]
    public int MaxRetries { get; set; } = 2;
}
