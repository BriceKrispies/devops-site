using System.Text.Json.Serialization;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Standardized API error response contract.
/// All non-success responses use this shape for consistency.
/// Constitution §9: Errors are explicit, typed, and classified.
/// </summary>
public sealed record ApiErrorResponse
{
    /// <summary>Stable machine-readable error code (e.g. "VALIDATION", "NOT_FOUND").</summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>Safe user-facing message. Never contains stack traces or internal details.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Request correlation ID for cross-referencing with backend logs.</summary>
    [JsonPropertyName("correlationId")]
    public required string CorrelationId { get; init; }

    /// <summary>Optional safe details when additional context is appropriate.</summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; init; }

    /// <summary>Field-level validation errors. Only present for validation failures.</summary>
    [JsonPropertyName("fieldErrors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? FieldErrors { get; init; }
}
