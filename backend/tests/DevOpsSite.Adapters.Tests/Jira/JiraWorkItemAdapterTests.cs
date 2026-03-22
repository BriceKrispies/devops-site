using System.Net;
using System.Text.Json;
using DevOpsSite.Adapters.Jira;
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Adapters.Tests.Jira;

/// <summary>
/// Jira adapter certification tests with controlled HTTP stubs.
/// Constitution §6.2D: Every failure mode mapped, observability asserted, no vendor leakage.
/// </summary>
public sealed class JiraWorkItemAdapterTests
{
    private readonly InMemoryTelemetryAdapter _telemetry = new();
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "jira-test-001",
        OperationName = "JiraAdapterTest",
        Timestamp = FixedTime,
        Source = OperationSource.HttpRequest
    };

    private JiraWorkItemAdapter CreateAdapter(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://jira.example.com") };
        return new JiraWorkItemAdapter(client, _telemetry);
    }

    private static string ValidJiraJson(string key = "PROJ-100", string summary = "Fix bug",
        string status = "Open", string? issueType = "Bug", string? assignee = "Alice") =>
        JsonSerializer.Serialize(new
        {
            key,
            fields = new
            {
                summary,
                status = new { name = status },
                issuetype = issueType is not null ? new { name = issueType } : null,
                assignee = assignee is not null ? new { displayName = assignee } : null
            }
        });

    // --- Happy path ---

    [Fact]
    public async Task Should_return_normalized_work_item_on_success()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, ValidJiraJson()));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal("PROJ-100", result.Value.Key.Value);
        Assert.Equal("Fix bug", result.Value.Title);
        Assert.Equal("Open", result.Value.Status);
        Assert.Equal("Bug", result.Value.Category);
        Assert.Equal("Alice", result.Value.Assignee);
        Assert.Equal("jira", result.Value.Provider);
        Assert.Contains("/browse/PROJ-100", result.Value.Url);
    }

    [Fact]
    public async Task Should_handle_null_optional_fields_in_response()
    {
        var json = ValidJiraJson(issueType: null, assignee: null);
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, json));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Category);
        Assert.Null(result.Value.Assignee);
    }

    // --- Failure mode: 404 Not Found ---

    [Fact]
    public async Task Should_return_not_found_on_404()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.NotFound));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("NOPE-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    // --- Failure mode: 401 / 403 Authorization ---

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Should_return_authorization_error_on_auth_failure(HttpStatusCode statusCode)
    {
        var adapter = CreateAdapter(new StubHandler(statusCode));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Authorization, result.Error.Code);
    }

    // --- Failure mode: 429 Rate Limited ---

    [Fact]
    public async Task Should_return_rate_limited_on_429()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.TooManyRequests));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.RateLimited, result.Error.Code);
    }

    // --- Failure mode: 5xx Server Error ---

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Should_return_transient_failure_on_5xx(HttpStatusCode statusCode)
    {
        var adapter = CreateAdapter(new StubHandler(statusCode));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.TransientFailure, result.Error.Code);
    }

    // --- Failure mode: Unexpected status code ---

    [Fact]
    public async Task Should_return_permanent_failure_on_unexpected_status()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.Gone));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.PermanentFailure, result.Error.Code);
    }

    // --- Failure mode: Timeout ---

    [Fact]
    public async Task Should_return_timeout_on_task_canceled_without_ct()
    {
        var adapter = CreateAdapter(new TimeoutHandler());

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Timeout, result.Error.Code);
    }

    // --- Failure mode: Cancellation ---

    [Fact]
    public async Task Should_return_timeout_on_cancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var adapter = CreateAdapter(new CancellationHandler());

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx(), cts.Token);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Timeout, result.Error.Code);
    }

    // --- Failure mode: Network unreachable ---

    [Fact]
    public async Task Should_return_dependency_unavailable_on_network_error()
    {
        var adapter = CreateAdapter(new NetworkErrorHandler());

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.DependencyUnavailable, result.Error.Code);
    }

    // --- Failure mode: Malformed JSON ---

    [Fact]
    public async Task Should_return_permanent_failure_on_malformed_json()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, "not-json{{{"));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.PermanentFailure, result.Error.Code);
    }

    // --- Failure mode: Null fields in JSON ---

    [Fact]
    public async Task Should_return_permanent_failure_on_null_fields()
    {
        var json = JsonSerializer.Serialize(new { key = "PROJ-1", fields = (object?)null });
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, json));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.PermanentFailure, result.Error.Code);
    }

    // --- Observability assertions ---

    [Fact]
    public async Task Should_emit_span_with_dependency_name_on_success()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, ValidJiraJson()));

        await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == "jira.GetByKey" && s.Result == "success");
    }

    [Fact]
    public async Task Should_emit_span_with_error_on_failure()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.NotFound));

        await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.Contains(_telemetry.Spans, s =>
            s.OperationName == "jira.GetByKey" && s.ErrorCode == ErrorCode.NotFound.ToString());
    }

    [Fact]
    public async Task Should_increment_success_counter()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, ValidJiraJson()));

        await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "external.calls"
            && c.Labels != null
            && c.Labels.TryGetValue("externalTarget", out var t) && t == "jira"
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_increment_failure_counter()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.NotFound));

        await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.Contains(_telemetry.Counters, c =>
            c.MetricName == "external.calls"
            && c.Labels != null
            && c.Labels.TryGetValue("externalTarget", out var t) && t == "jira"
            && c.Labels.TryGetValue("result", out var r) && r == "failure");
    }

    [Fact]
    public async Task Should_log_error_with_dependency_name_on_failure()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.NotFound));

        await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-1"), Ctx());

        Assert.Contains(_telemetry.Logs, l =>
            l.Level == "Error"
            && l.Dependency == "jira"
            && l.ErrorCode == ErrorCode.NotFound.ToString());
    }

    [Fact]
    public async Task Should_set_external_target_attribute_on_span()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, ValidJiraJson()));

        await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.Contains(_telemetry.Spans, s =>
            s.Attributes.TryGetValue("externalTarget", out var v) && v == "jira");
    }

    // --- No vendor leakage assertions ---

    [Fact]
    public async Task Result_should_not_contain_vendor_specific_types()
    {
        var adapter = CreateAdapter(new StubHandler(HttpStatusCode.OK, ValidJiraJson()));

        var result = await adapter.GetByKeyAsync(WorkItemKey.Create("PROJ-100"), Ctx());

        Assert.True(result.IsSuccess);
        var type = result.Value.GetType();
        Assert.DoesNotContain("Jira", type.FullName!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("jira", result.Value.Provider);
    }

    // --- Stub HTTP handlers ---

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public StubHandler(HttpStatusCode statusCode, string? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content is not null)
                response.Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new TaskCanceledException("Simulated timeout", new TimeoutException());
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class NetworkErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("Simulated network failure");
    }
}
