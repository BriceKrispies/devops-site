using System.Collections.Concurrent;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.Telemetry;

/// <summary>
/// In-memory telemetry adapter for tests and local development.
/// Collects all telemetry events for assertion.
/// </summary>
public sealed class InMemoryTelemetryAdapter : ITelemetryPort
{
    public ConcurrentBag<LogEntry> Logs { get; } = new();
    public ConcurrentBag<SpanRecord> Spans { get; } = new();
    public ConcurrentBag<CounterEntry> Counters { get; } = new();
    public ConcurrentBag<HistogramEntry> Histograms { get; } = new();

    public void LogInfo(string operationName, string correlationId, string message, IReadOnlyDictionary<string, object>? fields = null) =>
        Logs.Add(new LogEntry("Info", operationName, correlationId, message, fields));

    public void LogWarn(string operationName, string correlationId, string message, IReadOnlyDictionary<string, object>? fields = null) =>
        Logs.Add(new LogEntry("Warn", operationName, correlationId, message, fields));

    public void LogError(string operationName, string correlationId, string message, string? errorCode = null, string? dependency = null, IReadOnlyDictionary<string, object>? fields = null) =>
        Logs.Add(new LogEntry("Error", operationName, correlationId, message, fields, errorCode, dependency));

    public ISpan StartSpan(string operationName, string correlationId, string? parentSpanId = null)
    {
        var span = new InMemorySpan(operationName, correlationId, parentSpanId);
        span.OnDispose = s => Spans.Add(new SpanRecord(s.OperationName, s.CorrelationId, s.SpanId, s.Result, s.ErrorCode, s.ErrorMessage, s.Attributes));
        return span;
    }

    public void IncrementCounter(string metricName, IReadOnlyDictionary<string, string>? labels = null) =>
        Counters.Add(new CounterEntry(metricName, labels));

    public void RecordHistogram(string metricName, double value, IReadOnlyDictionary<string, string>? labels = null) =>
        Histograms.Add(new HistogramEntry(metricName, value, labels));

    public void Clear()
    {
        Logs.Clear();
        Spans.Clear();
        Counters.Clear();
        Histograms.Clear();
    }

    public sealed record LogEntry(string Level, string OperationName, string CorrelationId, string Message,
        IReadOnlyDictionary<string, object>? Fields = null, string? ErrorCode = null, string? Dependency = null);

    public sealed record SpanRecord(string OperationName, string CorrelationId, string SpanId,
        string? Result, string? ErrorCode, string? ErrorMessage, IReadOnlyDictionary<string, string> Attributes);

    public sealed record CounterEntry(string MetricName, IReadOnlyDictionary<string, string>? Labels);

    public sealed record HistogramEntry(string MetricName, double Value, IReadOnlyDictionary<string, string>? Labels);
}

internal sealed class InMemorySpan : ISpan
{
    public string SpanId { get; } = Guid.NewGuid().ToString("N")[..16];
    public string OperationName { get; }
    public string CorrelationId { get; }
    public string? Result { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    private readonly Dictionary<string, string> _attributes = new();
    public IReadOnlyDictionary<string, string> Attributes => _attributes;
    internal Action<InMemorySpan>? OnDispose { get; set; }

    public InMemorySpan(string operationName, string correlationId, string? parentSpanId)
    {
        OperationName = operationName;
        CorrelationId = correlationId;
        if (parentSpanId is not null)
            _attributes["parentSpanId"] = parentSpanId;
    }

    public void SetResult(string result) => Result = result;
    public void SetError(string errorCode, string message) { ErrorCode = errorCode; ErrorMessage = message; }
    public void SetAttribute(string key, string value) => _attributes[key] = value;
    public void Dispose() => OnDispose?.Invoke(this);
}
