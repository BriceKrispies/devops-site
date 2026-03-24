using DevOpsSite.Host.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Tests for OIDC auth startup validation.
/// Constitution §13.5: Fail fast on missing OIDC config.
/// </summary>
public sealed class OidcAuthGuardTests
{
    [Fact]
    public void Oidc_mode_without_Authority_fails_startup()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            BuildServicesWithOidc(authority: null, clientId: "test-client"));

        Assert.Contains("Authority", ex.Message);
    }

    [Fact]
    public void Oidc_mode_without_ClientId_fails_startup()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            BuildServicesWithOidc(authority: "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_test", clientId: null));

        Assert.Contains("ClientId", ex.Message);
    }

    [Fact]
    public void Oidc_mode_with_empty_Authority_fails_startup()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            BuildServicesWithOidc(authority: "", clientId: "test-client"));

        Assert.Contains("Authority", ex.Message);
    }

    [Fact]
    public void Oidc_mode_with_valid_config_builds_successfully()
    {
        var services = BuildServicesWithOidc(
            authority: "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_test",
            clientId: "test-client-id");

        // Should resolve successfully
        var provider = services.BuildServiceProvider();
        provider.ValidateCapabilityRegistry();
    }

    private static IServiceCollection BuildServicesWithOidc(string? authority, string? clientId)
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Auth:Mode"] = "Oidc",
            ["Auth:ActivePersona"] = "viewer",
            ["ServiceHealth:BaseUrl"] = "https://health-api.example.com",
            ["ServiceHealth:TimeoutMs"] = "5000",
            ["ServiceHealth:MaxRetries"] = "2",
            ["Jira:BaseUrl"] = "https://jira.example.com",
            ["Jira:CredentialsRef"] = "test-token",
            ["Jira:TimeoutMs"] = "10000",
            ["Jira:MaxRetries"] = "2",
            ["TraceStore:Provider"] = "InMemory",
            ["DynamoDb:UsersTableName"] = "test-users",
            ["DynamoDb:RolesTableName"] = "test-roles",
            ["DynamoDb:Region"] = "us-east-1"
        };

        if (authority is not null)
            configDict["Auth:Authority"] = authority;
        if (clientId is not null)
            configDict["Auth:ClientId"] = clientId;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Production"));
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
