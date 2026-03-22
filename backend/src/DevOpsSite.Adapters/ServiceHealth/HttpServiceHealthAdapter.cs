using System.Net;
using System.Text.Json;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.ServiceHealth;

/// <summary>
/// HTTP adapter that fetches service health from an external monitoring endpoint.
/// Constitution §10: vendor-specific errors mapped to internal taxonomy.
/// </summary>
public sealed class HttpServiceHealthAdapter : IServiceHealthPort
{
    private readonly HttpClient _httpClient;
    private readonly ITelemetryPort _telemetry;
    private const string DependencyName = "service-health-api";

    public HttpServiceHealthAdapter(HttpClient httpClient, ITelemetryPort telemetry)
    {
        _httpClient = httpClient;
        _telemetry = telemetry;
    }

    public async Task<Result<ServiceHealthSummary>> GetHealthAsync(ServiceId serviceId, OperationContext ctx, CancellationToken ct = default)
    {
        using var span = _telemetry.StartSpan($"{DependencyName}.GetHealth", ctx.CorrelationId);
        span.SetAttribute("externalTarget", DependencyName);
        span.SetAttribute("serviceId", serviceId.Value);

        try
        {
            var response = await _httpClient.GetAsync($"/health/{serviceId.Value}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                span.SetResult("not_found");
                return Result<ServiceHealthSummary>.Failure(
                    AppError.NotFound($"Service '{serviceId.Value}' not found.", ctx.OperationName, ctx.CorrelationId));
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                span.SetError(ErrorCode.RateLimited.ToString(), "Rate limited by health API");
                return Result<ServiceHealthSummary>.Failure(
                    AppError.TransientFailure("Rate limited by health API.", ctx.OperationName, ctx.CorrelationId, DependencyName));
            }

            if (!response.IsSuccessStatusCode)
            {
                var code = (int)response.StatusCode >= 500 ? ErrorCode.TransientFailure : ErrorCode.PermanentFailure;
                span.SetError(code.ToString(), $"HTTP {(int)response.StatusCode}");
                _telemetry.IncrementCounter("external.calls", new Dictionary<string, string>
                {
                    ["externalTarget"] = DependencyName, ["result"] = "failure"
                });
                return Result<ServiceHealthSummary>.Failure(new AppError
                {
                    Code = code,
                    Message = $"Health API returned HTTP {(int)response.StatusCode}.",
                    Severity = Application.Errors.Severity.Error,
                    OperationName = ctx.OperationName,
                    CorrelationId = ctx.CorrelationId,
                    Dependency = DependencyName
                });
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<HealthDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null)
                return Result<ServiceHealthSummary>.Failure(
                    AppError.PermanentFailure("Invalid response from health API.", ctx.OperationName, ctx.CorrelationId, DependencyName));

            var status = dto.Status?.ToLowerInvariant() switch
            {
                "healthy" => HealthStatus.Healthy,
                "degraded" => HealthStatus.Degraded,
                "unhealthy" => HealthStatus.Unhealthy,
                _ => HealthStatus.Unknown
            };

            var summary = ServiceHealthSummary.Create(
                serviceId, status, dto.Description ?? "", DateTimeOffset.Parse(dto.CheckedAt ?? DateTimeOffset.UtcNow.ToString("o")));

            span.SetResult("success");
            _telemetry.IncrementCounter("external.calls", new Dictionary<string, string>
            {
                ["externalTarget"] = DependencyName, ["result"] = "success"
            });

            return Result<ServiceHealthSummary>.Success(summary);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ct.IsCancellationRequested == false)
        {
            span.SetError(ErrorCode.Timeout.ToString(), "Request timed out");
            _telemetry.IncrementCounter("external.calls", new Dictionary<string, string>
            {
                ["externalTarget"] = DependencyName, ["result"] = "timeout"
            });
            return Result<ServiceHealthSummary>.Failure(
                AppError.Timeout("Health API request timed out.", ctx.OperationName, ctx.CorrelationId, DependencyName, ex));
        }
        catch (HttpRequestException ex)
        {
            span.SetError(ErrorCode.DependencyUnavailable.ToString(), ex.Message);
            _telemetry.IncrementCounter("external.calls", new Dictionary<string, string>
            {
                ["externalTarget"] = DependencyName, ["result"] = "unavailable"
            });
            return Result<ServiceHealthSummary>.Failure(
                AppError.DependencyUnavailable("Health API unreachable.", ctx.OperationName, ctx.CorrelationId, DependencyName, ex));
        }
    }

    private sealed record HealthDto
    {
        public string? Status { get; init; }
        public string? Description { get; init; }
        public string? CheckedAt { get; init; }
    }
}
