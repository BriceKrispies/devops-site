using DevOpsSite.Adapters.Capabilities;
using DevOpsSite.Adapters.Configuration;
using DevOpsSite.Adapters.Jira;
using DevOpsSite.Adapters.ServiceHealth;
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Adapters.TraceStore;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.UseCases;
using DevOpsSite.Host.Authentication;

namespace DevOpsSite.Host.Composition;

/// <summary>
/// DI composition root. Constitution §3.4 Host: DI composition, config binding, runtime wiring.
/// Constitution §14: Capability registry populated and validated at startup.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Auth config — Constitution §11, §14
        var authConfig = new AuthConfig();
        configuration.GetSection("Auth").Bind(authConfig);
        Validator.ValidateConfig(authConfig);
        services.AddSingleton(authConfig);

        // Typed config — Constitution §11
        var healthConfig = new ServiceHealthConfig();
        configuration.GetSection("ServiceHealth").Bind(healthConfig);
        Validator.ValidateConfig(healthConfig);
        services.AddSingleton(healthConfig);

        // Ports → Adapters — Constitution §10
        services.AddSingleton<ITelemetryPort, InMemoryTelemetryAdapter>();
        services.AddSingleton<IAuditPort, InMemoryAuditAdapter>();
        services.AddSingleton<IClockPort, SystemClockAdapter>();

        services.AddHttpClient<IServiceHealthPort, HttpServiceHealthAdapter>(client =>
        {
            client.BaseAddress = new Uri(healthConfig.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(healthConfig.TimeoutMs);
        });

        // Jira adapter — Constitution §10
        var jiraConfig = new JiraConfig();
        configuration.GetSection("Jira").Bind(jiraConfig);
        Validator.ValidateConfig(jiraConfig);
        services.AddSingleton(jiraConfig);

        services.AddHttpClient<IWorkItemPort, JiraWorkItemAdapter>(client =>
        {
            client.BaseAddress = new Uri(jiraConfig.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(jiraConfig.TimeoutMs);
        });

        // Trace store — Constitution §10: centralized backing-store selection
        var traceStoreConfig = new TraceStoreConfig();
        configuration.GetSection("TraceStore").Bind(traceStoreConfig);
        Validator.ValidateConfig(traceStoreConfig);
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

        // Ingestion source — Constitution §10: default in-memory for local dev
        services.AddSingleton<ITraceIngestionSourcePort, InMemoryTraceIngestionSourceAdapter>();

        // Capability override store — for kill switches and runtime overrides
        services.AddSingleton<ICapabilityOverrideStore, InMemoryCapabilityOverrideStore>();

        // Authorization — Constitution §14: Deny by default
        var registry = new CapabilityRegistry();
        registry.Register(GetServiceHealthHandler.Descriptor);
        registry.Register(GetWorkItemHandler.Descriptor);
        registry.Register(AddTraceEventsHandler.Descriptor);
        registry.Register(QueryTraceEventsHandler.Descriptor);
        registry.Register(IngestTraceEventsHandler.Descriptor);
        services.AddSingleton(registry);
        services.AddSingleton<IAuthorizationService>(sp =>
            new AuthorizationService(
                sp.GetRequiredService<CapabilityRegistry>(),
                sp.GetRequiredService<ICapabilityOverrideStore>()));

        // Capability resolution — resolves status for all capabilities per user/context
        services.AddSingleton<ICapabilityResolutionService, CapabilityResolutionService>();

        // Use cases
        services.AddScoped<GetServiceHealthHandler>();
        services.AddScoped<GetWorkItemHandler>();
        services.AddScoped<AddTraceEventsHandler>();
        services.AddScoped<QueryTraceEventsHandler>();
        services.AddScoped<IngestTraceEventsHandler>();

        return services;
    }

    /// <summary>
    /// Startup validation: auth guard + capability registry.
    /// Constitution §13.5: Fail fast on invalid state.
    /// Constitution §14: Missing auth metadata = startup failure.
    /// </summary>
    public static void ValidateCapabilityRegistry(this IServiceProvider provider)
    {
        // Hard guard: DevelopmentBypass is ONLY allowed in Development environment
        var authConfig = provider.GetRequiredService<AuthConfig>();
        var environment = provider.GetRequiredService<IHostEnvironment>();
        if (AuthMode.IsDevelopmentBypass(authConfig.Mode) && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"SECURITY: Auth mode '{AuthMode.DevelopmentBypass}' is not allowed in '{environment.EnvironmentName}' environment. " +
                "DevelopmentBypass auth is restricted to the Development environment only. " +
                "Set Auth:Mode to a production-safe value (e.g., 'Oidc').");
        }

        if (AuthMode.IsDevelopmentBypass(authConfig.Mode))
        {
            // Validate the configured persona exists
            _ = DevPersonas.GetPersona(authConfig.ActivePersona);
        }

        var registry = provider.GetRequiredService<CapabilityRegistry>();
        var registeredOps = registry.GetRegisteredOperations();

        // Validate all descriptors are internally consistent
        foreach (var descriptor in registry.GetAll())
        {
            var errors = descriptor.Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Capability descriptor '{descriptor.OperationName}' is invalid: {string.Join("; ", errors)}");
        }

        // Verify known handlers are registered
        var requiredHandlers = new[]
        {
            GetServiceHealthHandler.OperationName,
            GetWorkItemHandler.OperationName,
            AddTraceEventsHandler.OperationName,
            QueryTraceEventsHandler.OperationName,
            IngestTraceEventsHandler.OperationName
        };

        var missing = requiredHandlers.Where(h => !registeredOps.Contains(h)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing capability descriptors for handlers: {string.Join(", ", missing)}. " +
                "Every handler must declare and register a CapabilityDescriptor.");
    }
}

internal static class Validator
{
    /// <summary>
    /// Fail-fast config validation at startup. Constitution §13.5.
    /// </summary>
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
