using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Adapters.Configuration;

/// <summary>
/// Configuration for trace store backing provider.
/// Constitution §11: Typed, validated configuration.
/// </summary>
public sealed class TraceStoreConfig
{
    /// <summary>
    /// Backing store provider. Currently supported: "InMemory".
    /// Future: "Postgres", "Elasticsearch", etc.
    /// </summary>
    [Required]
    public string Provider { get; set; } = "InMemory";
}
