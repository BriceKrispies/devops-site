namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for structured logging, metrics, and span emission.
/// Constitution §7: Observability is required for every meaningful operation.
/// </summary>
public interface ITelemetryPort
{
    void LogInfo(string operationName, string correlationId, string message, IReadOnlyDictionary<string, object>? fields = null);
    void LogWarn(string operationName, string correlationId, string message, IReadOnlyDictionary<string, object>? fields = null);
    void LogError(string operationName, string correlationId, string message, string? errorCode = null, string? dependency = null, IReadOnlyDictionary<string, object>? fields = null);

    ISpan StartSpan(string operationName, string correlationId, string? parentSpanId = null);

    void IncrementCounter(string metricName, IReadOnlyDictionary<string, string>? labels = null);
    void RecordHistogram(string metricName, double value, IReadOnlyDictionary<string, string>? labels = null);
}

/// <summary>
/// Represents a trace span. Dispose to end the span.
/// </summary>
public interface ISpan : IDisposable
{
    string SpanId { get; }
    void SetResult(string result);
    void SetError(string errorCode, string message);
    void SetAttribute(string key, string value);
}
