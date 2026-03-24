using DevOpsSite.Adapters.Configuration;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Host.Authentication;
using DevOpsSite.Host.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Tests for the DevelopmentBypass auth guard.
/// Constitution §13.5: Fail fast on invalid state.
/// Dev auth must be rejected outside Development environment.
/// </summary>
public sealed class DevelopmentAuthGuardTests
{
    // --- Startup guard: DevelopmentBypass rejected outside Development ---

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public void DevBypass_must_fail_startup_outside_Development(string environmentName)
    {
        var services = BuildServicesWithAuth("DevelopmentBypass", "viewer", environmentName);
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.ValidateCapabilityRegistry());

        Assert.Contains("DevelopmentBypass", ex.Message);
        Assert.Contains("not allowed", ex.Message);
    }

    [Fact]
    public void DevBypass_must_succeed_in_Development()
    {
        var services = BuildServicesWithAuth("DevelopmentBypass", "viewer", "Development");
        var provider = services.BuildServiceProvider();

        // Should not throw
        provider.ValidateCapabilityRegistry();
    }

    [Fact]
    public void Oidc_mode_does_not_trigger_guard_in_Production()
    {
        var services = BuildServicesWithAuth("Oidc", "viewer", "Production",
            oidcAuthority: "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_test",
            oidcClientId: "test-client");
        var provider = services.BuildServiceProvider();

        // Should not throw — Oidc mode is allowed everywhere
        provider.ValidateCapabilityRegistry();
    }

    // --- Persona validation at startup ---

    [Fact]
    public void Invalid_persona_must_fail_startup()
    {
        var services = BuildServicesWithAuth("DevelopmentBypass", "nonexistent", "Development");
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.ValidateCapabilityRegistry());

        Assert.Contains("nonexistent", ex.Message);
    }

    [Theory]
    [InlineData("viewer")]
    [InlineData("operator")]
    [InlineData("admin")]
    public void Valid_personas_pass_startup(string persona)
    {
        var services = BuildServicesWithAuth("DevelopmentBypass", persona, "Development");
        var provider = services.BuildServiceProvider();

        // Should not throw
        provider.ValidateCapabilityRegistry();
    }

    // --- Persona mapping into internal auth model ---

    [Fact]
    public void Viewer_persona_maps_to_readonly_permissions()
    {
        var persona = DevPersonas.GetPersona("viewer");

        Assert.Equal("dev:viewer", persona.Id);
        Assert.Contains(Permission.WellKnown.ServiceHealthRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.WorkItemRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsRead, persona.Permissions);
        Assert.DoesNotContain(Permission.WellKnown.TraceEventsWrite, persona.Permissions);
        Assert.DoesNotContain(Permission.WellKnown.TraceEventsIngest, persona.Permissions);
    }

    [Fact]
    public void Operator_persona_maps_to_readwrite_permissions()
    {
        var persona = DevPersonas.GetPersona("operator");

        Assert.Equal("dev:operator", persona.Id);
        Assert.Contains(Permission.WellKnown.ServiceHealthRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsWrite, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsIngest, persona.Permissions);
    }

    [Fact]
    public void Admin_persona_maps_to_all_permissions()
    {
        var persona = DevPersonas.GetPersona("admin");

        Assert.Equal("dev:admin", persona.Id);
        Assert.Contains(Permission.WellKnown.ServiceHealthRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.WorkItemRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsRead, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsWrite, persona.Permissions);
        Assert.Contains(Permission.WellKnown.TraceEventsIngest, persona.Permissions);
    }

    [Fact]
    public void Persona_produces_valid_ActorIdentity()
    {
        var persona = DevPersonas.GetPersona("operator");
        var actor = persona.ToActor();

        Assert.Equal("dev:operator", actor.Id);
        Assert.Equal(ActorType.User, actor.Type);
        Assert.Equal("Dev Operator", actor.DisplayName);
    }

    [Fact]
    public void Persona_permissions_work_with_AuthorizationService()
    {
        var persona = DevPersonas.GetPersona("viewer");

        var registry = new CapabilityRegistry();
        registry.Register(DevOpsSite.Application.UseCases.GetServiceHealthHandler.Descriptor);
        var authz = new AuthorizationService(registry);

        var ctx = new OperationContext
        {
            CorrelationId = "test-dev-auth",
            OperationName = DevOpsSite.Application.UseCases.GetServiceHealthHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = persona.ToActor(),
            Permissions = persona.Permissions
        };

        var result = authz.Evaluate(
            DevOpsSite.Application.UseCases.GetServiceHealthHandler.OperationName, ctx);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Viewer_persona_is_denied_write_capability()
    {
        var persona = DevPersonas.GetPersona("viewer");

        var registry = new CapabilityRegistry();
        registry.Register(DevOpsSite.Application.UseCases.AddTraceEventsHandler.Descriptor);
        var authz = new AuthorizationService(registry);

        var ctx = new OperationContext
        {
            CorrelationId = "test-dev-auth",
            OperationName = DevOpsSite.Application.UseCases.AddTraceEventsHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = OperationSource.HttpRequest,
            Actor = persona.ToActor(),
            Permissions = persona.Permissions
        };

        var result = authz.Evaluate(
            DevOpsSite.Application.UseCases.AddTraceEventsHandler.OperationName, ctx);

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.Forbidden, result.FailureReason);
    }

    [Fact]
    public void Persona_lookup_is_case_insensitive()
    {
        var lower = DevPersonas.GetPersona("viewer");
        var upper = DevPersonas.GetPersona("Viewer");
        var mixed = DevPersonas.GetPersona("VIEWER");

        Assert.Equal(lower.Id, upper.Id);
        Assert.Equal(lower.Id, mixed.Id);
    }

    [Fact]
    public void AuthMode_detection_is_case_insensitive()
    {
        Assert.True(AuthMode.IsDevelopmentBypass("DevelopmentBypass"));
        Assert.True(AuthMode.IsDevelopmentBypass("developmentbypass"));
        Assert.True(AuthMode.IsDevelopmentBypass("DEVELOPMENTBYPASS"));
        Assert.False(AuthMode.IsDevelopmentBypass("Oidc"));
        Assert.False(AuthMode.IsDevelopmentBypass(""));
    }

    // --- Helper: builds a minimal service collection with auth config ---

    private static IServiceCollection BuildServicesWithAuth(
        string mode, string persona, string environmentName,
        string? oidcAuthority = null, string? oidcClientId = null)
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Auth:Mode"] = mode,
            ["Auth:ActivePersona"] = persona,
            ["ServiceHealth:BaseUrl"] = "https://health-api.example.com",
            ["ServiceHealth:TimeoutMs"] = "5000",
            ["ServiceHealth:MaxRetries"] = "2",
            ["Jira:BaseUrl"] = "https://jira.example.com",
            ["Jira:CredentialsRef"] = "test-token",
            ["Jira:TimeoutMs"] = "10000",
            ["Jira:MaxRetries"] = "2",
            ["TraceStore:Provider"] = "InMemory"
        };

        if (oidcAuthority is not null)
        {
            configDict["Auth:Authority"] = oidcAuthority;
            configDict["DynamoDb:UsersTableName"] = "test-users";
            configDict["DynamoDb:RolesTableName"] = "test-roles";
            configDict["DynamoDb:Region"] = "us-east-1";
        }
        if (oidcClientId is not null)
            configDict["Auth:ClientId"] = oidcClientId;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environmentName));
        services.AddBackendServices(config);
        return services;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "DevOpsSite.Host.Tests";
            ContentRootPath = "";
            ContentRootFileProvider = null!;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
    }
}
