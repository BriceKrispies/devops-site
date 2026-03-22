using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevOpsSite.Worker.Services;

/// <summary>
/// Background service that periodically polls the trace ingestion source
/// and stores events via the IngestTraceEvents application capability.
/// Constitution §12: Transport shell only — parse, invoke use case, handle result.
/// No business logic here.
/// </summary>
public sealed class TraceIngestionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TraceIngestionConfig _config;
    private readonly ITelemetryPort _telemetry;
    private readonly IClockPort _clock;

    public const string ServiceName = "TraceIngestionService";

    public TraceIngestionService(
        IServiceProvider serviceProvider,
        TraceIngestionConfig config,
        ITelemetryPort telemetry,
        IClockPort clock)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _telemetry.LogInfo(ServiceName, "worker-startup",
            $"TraceIngestionService starting. PollInterval={_config.PollIntervalSeconds}s, MaxBatch={_config.MaxBatchSize}.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIngestionCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — expected
                break;
            }
            catch (Exception ex)
            {
                // Unexpected failure — log and continue. Do not crash the worker.
                _telemetry.LogError(ServiceName, "worker-cycle",
                    $"Unexpected error in ingestion cycle: {ex.Message}",
                    "InvariantViolation");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _telemetry.LogInfo(ServiceName, "worker-shutdown",
            "TraceIngestionService stopped.");
    }

    private async Task RunIngestionCycleAsync(CancellationToken ct)
    {
        var correlationId = $"ingest-{_clock.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

        var ctx = new OperationContext
        {
            CorrelationId = correlationId,
            OperationName = IngestTraceEventsHandler.OperationName,
            Timestamp = _clock.UtcNow,
            Source = OperationSource.BackgroundJob,
            Actor = new ActorIdentity
            {
                Id = "worker:trace-ingestion",
                Type = ActorType.Service,
                DisplayName = "Trace Ingestion Worker"
            },
            Permissions = new HashSet<Permission> { Permission.WellKnown.TraceEventsIngest }
        };

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IngestTraceEventsHandler>();

        var command = new IngestTraceEventsCommand { MaxBatchSize = _config.MaxBatchSize };
        var result = await handler.HandleAsync(command, ctx, ct);

        if (result.IsFailure)
        {
            _telemetry.LogError(ServiceName, correlationId,
                $"Ingestion cycle failed: {result.Error.Message}",
                result.Error.Code.ToString());
        }
    }
}
