using System.Net;
using System.Text.Json;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.Jira;

/// <summary>
/// Jira adapter implementing IWorkItemPort.
/// Constitution §10: vendor-specific errors mapped to internal taxonomy.
/// No Jira DTOs, exceptions, or error semantics leak past this boundary.
/// </summary>
public sealed class JiraWorkItemAdapter : IWorkItemPort
{
    private readonly HttpClient _httpClient;
    private readonly ITelemetryPort _telemetry;
    private const string DependencyName = "jira";
    private const string Provider = "jira";

    public JiraWorkItemAdapter(HttpClient httpClient, ITelemetryPort telemetry)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    public async Task<Result<WorkItemSummary>> GetByKeyAsync(WorkItemKey key, OperationContext ctx, CancellationToken ct = default)
    {
        using var span = _telemetry.StartSpan($"{DependencyName}.GetByKey", ctx.CorrelationId);
        span.SetAttribute("externalTarget", DependencyName);
        span.SetAttribute("workItemKey", key.Value);

        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"/rest/api/2/issue/{key.Value}?fields=summary,status,issuetype,assignee");
            request.Headers.Add("X-Correlation-Id", ctx.CorrelationId);

            response = await _httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return Fail(span, ctx, ErrorCode.Timeout,
                "Jira request timed out.", ex);
        }
        catch (TaskCanceledException)
        {
            return Fail(span, ctx, ErrorCode.Timeout,
                "Jira request was cancelled.", null);
        }
        catch (HttpRequestException ex)
        {
            return Fail(span, ctx, ErrorCode.DependencyUnavailable,
                "Jira is unreachable.", ex);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ParseSuccessResponse(response, key, ctx, span, ct),

            HttpStatusCode.NotFound =>
                Fail(span, ctx, ErrorCode.NotFound,
                    $"Work item '{key.Value}' not found in Jira."),

            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                Fail(span, ctx, ErrorCode.Authorization,
                    "Jira authentication/authorization failed."),

            HttpStatusCode.TooManyRequests =>
                Fail(span, ctx, ErrorCode.RateLimited,
                    "Jira rate limit exceeded."),

            _ when (int)response.StatusCode >= 500 =>
                Fail(span, ctx, ErrorCode.TransientFailure,
                    $"Jira returned HTTP {(int)response.StatusCode}."),

            _ =>
                Fail(span, ctx, ErrorCode.PermanentFailure,
                    $"Jira returned unexpected HTTP {(int)response.StatusCode}.")
        };
    }

    private async Task<Result<WorkItemSummary>> ParseSuccessResponse(
        HttpResponseMessage response, WorkItemKey key, OperationContext ctx, ISpan span, CancellationToken ct)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return Fail(span, ctx, ErrorCode.PermanentFailure,
                "Failed to read Jira response body.", ex);
        }

        JiraIssueDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<JiraIssueDto>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Fail(span, ctx, ErrorCode.PermanentFailure,
                "Jira returned malformed JSON.", ex);
        }

        if (dto?.Fields is null)
        {
            return Fail(span, ctx, ErrorCode.PermanentFailure,
                "Jira returned an invalid issue payload.");
        }

        var issueKey = dto.Key ?? key.Value;
        var summary = WorkItemSummary.Create(
            WorkItemKey.Create(issueKey),
            title: dto.Fields.Summary ?? "(no summary)",
            status: dto.Fields.Status?.Name ?? "Unknown",
            category: dto.Fields.IssueType?.Name,
            assignee: dto.Fields.Assignee?.DisplayName,
            url: _httpClient.BaseAddress is not null
                ? $"{_httpClient.BaseAddress.ToString().TrimEnd('/')}/browse/{issueKey}"
                : null,
            provider: Provider,
            retrievedAt: DateTimeOffset.UtcNow);

        span.SetResult("success");
        _telemetry.IncrementCounter("external.calls", new Dictionary<string, string>
        {
            ["externalTarget"] = DependencyName, ["result"] = "success"
        });

        return Result<WorkItemSummary>.Success(summary);
    }

    private Result<WorkItemSummary> Fail(ISpan span, OperationContext ctx,
        ErrorCode code, string message, Exception? cause = null)
    {
        var severity = code switch
        {
            ErrorCode.NotFound => Severity.Info,
            ErrorCode.Authorization or ErrorCode.RateLimited => Severity.Warn,
            _ => Severity.Error
        };

        span.SetError(code.ToString(), message);
        _telemetry.IncrementCounter("external.calls", new Dictionary<string, string>
        {
            ["externalTarget"] = DependencyName, ["result"] = "failure"
        });
        _telemetry.LogError(ctx.OperationName, ctx.CorrelationId,
            message, code.ToString(), DependencyName);

        return Result<WorkItemSummary>.Failure(new AppError
        {
            Code = code,
            Message = message,
            Severity = severity,
            OperationName = ctx.OperationName,
            CorrelationId = ctx.CorrelationId,
            Dependency = DependencyName,
            Cause = cause
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Internal DTOs — never leave this file. They exist only to deserialize Jira JSON.
    private sealed record JiraIssueDto
    {
        public string? Key { get; init; }
        public JiraFieldsDto? Fields { get; init; }
    }

    private sealed record JiraFieldsDto
    {
        public string? Summary { get; init; }
        public JiraStatusDto? Status { get; init; }
        public JiraIssueTypeDto? IssueType { get; init; }
        public JiraAssigneeDto? Assignee { get; init; }
    }

    private sealed record JiraStatusDto { public string? Name { get; init; } }
    private sealed record JiraIssueTypeDto { public string? Name { get; init; } }
    private sealed record JiraAssigneeDto { public string? DisplayName { get; init; } }
}
