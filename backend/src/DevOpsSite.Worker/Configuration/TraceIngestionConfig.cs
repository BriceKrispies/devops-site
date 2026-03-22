using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Worker.Configuration;

/// <summary>
/// Configuration for the trace ingestion background service.
/// Constitution §11: Typed, validated configuration.
/// </summary>
public sealed class TraceIngestionConfig
{
    /// <summary>Whether the trace ingestion service is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Poll interval in seconds between ingestion cycles.</summary>
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Maximum number of events to fetch per ingestion cycle.</summary>
    [Range(1, 500)]
    public int MaxBatchSize { get; set; } = 100;
}
