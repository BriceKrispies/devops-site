using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Worker.Configuration;
using DevOpsSite.Worker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsSite.Worker.Tests.Services;

/// <summary>
/// Behavior tests for TraceIngestionService.
/// Verifies that the hosted service invokes the application capability correctly,
/// handles cancellation, and respects configuration.
/// </summary>
public sealed class TraceIngestionServiceTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);

    private static (TraceIngestionService service, InMemoryTraceStoreAdapter store, InMemoryTraceIngestionSourceAdapter source, InMemoryTelemetryAdapter telemetry) CreateService(
        TraceIngestionConfig? config = null)
    {
        config ??= new TraceIngestionConfig { Enabled = true, PollIntervalSeconds = 1, MaxBatchSize = 50 };

        var telemetry = new InMemoryTelemetryAdapter();
        var clock = new FixedClockAdapter(FixedTime);
        var store = new InMemoryTraceStoreAdapter();
        var source = new InMemoryTraceIngestionSourceAdapter();

        var registry = new CapabilityRegistry();
        registry.Register(IngestTraceEventsHandler.Descriptor);
        var authz = new AuthorizationService(registry);

        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryPort>(telemetry);
        services.AddSingleton<IAuditPort, InMemoryAuditAdapter>();
        services.AddSingleton<IClockPort>(clock);
        services.AddSingleton<ITraceStorePort>(store);
        services.AddSingleton<ITraceIngestionSourcePort>(source);
        services.AddSingleton(registry);
        services.AddSingleton<IAuthorizationService>(authz);
        services.AddScoped<IngestTraceEventsHandler>();

        var serviceProvider = services.BuildServiceProvider();

        var service = new TraceIngestionService(serviceProvider, config, telemetry, clock);
        return (service, store, source, telemetry);
    }

    [Fact]
    public async Task Should_ingest_pending_events_from_source_into_store()
    {
        var (service, store, source, telemetry) = CreateService();

        source.Enqueue(new TraceEventInput
        {
            Id = "evt-worker-1",
            SourceSystem = "github-actions",
            EventType = "deployment",
            OccurredAt = FixedTime,
            DisplayTitle = "Deploy v1.0",
            ServiceName = "api-service"
        });

        using var cts = new CancellationTokenSource();

        // Start the service and let it run one cycle
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500); // Allow one poll cycle
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Event should be in the store
        var stored = store.GetAll();
        Assert.Single(stored);
        Assert.Equal("evt-worker-1", stored[0].Id.Value);
    }

    [Fact]
    public async Task Should_acknowledge_successfully_stored_events()
    {
        var (service, store, source, telemetry) = CreateService();

        source.Enqueue(new TraceEventInput
        {
            Id = "evt-ack-1",
            SourceSystem = "ci",
            EventType = "build",
            OccurredAt = FixedTime,
            DisplayTitle = "Build #42"
        });

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        var acked = source.GetAcknowledged();
        Assert.Contains("evt-ack-1", acked);
    }

    [Fact]
    public async Task Should_handle_empty_source_gracefully()
    {
        var (service, store, source, telemetry) = CreateService();

        // No events enqueued — source is empty

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        Assert.Empty(store.GetAll());

        // Should still emit telemetry for the cycle
        Assert.Contains(telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("operationName", out var op) && op == "IngestTraceEvents"
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }

    [Fact]
    public async Task Should_respect_cancellation_and_stop_cleanly()
    {
        var (service, store, source, telemetry) = CreateService(
            new TraceIngestionConfig { Enabled = true, PollIntervalSeconds = 60, MaxBatchSize = 50 });

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        // Cancel almost immediately
        await Task.Delay(100);
        cts.Cancel();

        // Should not throw
        await service.StopAsync(CancellationToken.None);

        // Should have emitted shutdown log
        Assert.Contains(telemetry.Logs, l =>
            l.OperationName == TraceIngestionService.ServiceName
            && l.Message.Contains("stopped"));
    }

    [Fact]
    public async Task Should_emit_startup_log()
    {
        var (service, store, source, telemetry) = CreateService();

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        Assert.Contains(telemetry.Logs, l =>
            l.OperationName == TraceIngestionService.ServiceName
            && l.Message.Contains("starting"));
    }

    [Fact]
    public async Task Should_emit_telemetry_on_successful_ingestion()
    {
        var (service, store, source, telemetry) = CreateService();

        source.Enqueue(new TraceEventInput
        {
            Id = "evt-tel-1",
            SourceSystem = "ci",
            EventType = "build",
            OccurredAt = FixedTime,
            DisplayTitle = "Build"
        });

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Span emitted
        Assert.Contains(telemetry.Spans, s =>
            s.OperationName == IngestTraceEventsHandler.OperationName && s.Result == "success");

        // Counter emitted
        Assert.Contains(telemetry.Counters, c =>
            c.MetricName == "capability.invocations"
            && c.Labels != null
            && c.Labels.TryGetValue("result", out var r) && r == "success");
    }
}
