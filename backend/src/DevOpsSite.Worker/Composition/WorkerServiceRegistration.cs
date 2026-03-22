using DevOpsSite.Adapters.Configuration;
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Worker.Configuration;
using DevOpsSite.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevOpsSite.Worker.Composition;

/// <summary>
/// DI composition root for the worker host.
/// Constitution §3.4 Host: DI composition, config binding, runtime wiring.
/// No business logic here — just wiring.
/// </summary>
public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Worker-specific config
        var ingestionConfig = new TraceIngestionConfig();
        configuration.GetSection("TraceIngestion").Bind(ingestionConfig);
        ConfigValidator.ValidateConfig(ingestionConfig);
        services.AddSingleton(ingestionConfig);

        // Shared infrastructure ports — Constitution §10
        services.AddSingleton<ITelemetryPort, InMemoryTelemetryAdapter>();
        services.AddSingleton<IAuditPort, InMemoryAuditAdapter>();
        services.AddSingleton<IClockPort, SystemClockAdapter>();

        // Trace store — Constitution §10: centralized backing-store selection
        var traceStoreConfig = new TraceStoreConfig();
        configuration.GetSection("TraceStore").Bind(traceStoreConfig);
        ConfigValidator.ValidateConfig(traceStoreConfig);
        services.AddSingleton(traceStoreConfig);

        if (string.Equals(traceStoreConfig.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ITraceStorePort, InMemoryTraceStoreAdapter>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Unknown TraceStore provider: '{traceStoreConfig.Provider}'. Supported: InMemory.");
        }

        // Ingestion source — in-memory for now, swappable per config later
        services.AddSingleton<ITraceIngestionSourcePort, InMemoryTraceIngestionSourceAdapter>();

        // Authorization — Constitution §14: Deny by default
        var registry = new CapabilityRegistry();
        registry.Register(IngestTraceEventsHandler.Descriptor);
        services.AddSingleton(registry);
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        // Use cases invoked by worker services
        services.AddScoped<IngestTraceEventsHandler>();

        // Background services
        if (ingestionConfig.Enabled)
        {
            services.AddHostedService<TraceIngestionService>();
        }

        return services;
    }

    /// <summary>
    /// Startup validation for the worker.
    /// Constitution §13.5: Fail fast on invalid state.
    /// </summary>
    public static void ValidateWorkerServices(this IServiceProvider provider)
    {
        var registry = provider.GetRequiredService<CapabilityRegistry>();

        foreach (var descriptor in registry.GetAll())
        {
            var errors = descriptor.Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Capability descriptor '{descriptor.OperationName}' is invalid: {string.Join("; ", errors)}");
        }

        // Verify the ingestion handler is registered
        var registeredOps = registry.GetRegisteredOperations();
        if (!registeredOps.Contains(IngestTraceEventsHandler.OperationName))
            throw new InvalidOperationException(
                $"Missing capability descriptor for {IngestTraceEventsHandler.OperationName}.");
    }
}

internal static class ConfigValidator
{
    public static void ValidateConfig<T>(T config) where T : class
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(config);
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(config, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Configuration validation failed for {typeof(T).Name}: {errors}");
        }
    }
}
