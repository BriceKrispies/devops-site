using NetArchTest.Rules;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Constitution §4: Dependency Law enforcement.
/// Domain -> nothing outside Domain
/// Application -> Domain only
/// Adapters -> Application + Domain only
/// </summary>
public sealed class DependencyDirectionTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Domain.ValueObjects.ServiceId).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.Results.Result<>).Assembly;
    private static readonly System.Reflection.Assembly AdaptersAssembly = typeof(Adapters.Telemetry.InMemoryTelemetryAdapter).Assembly;
    private static readonly System.Reflection.Assembly HostAssembly = typeof(Host.Composition.ServiceRegistration).Assembly;
    private static readonly System.Reflection.Assembly WorkerAssembly = typeof(Worker.Composition.WorkerServiceRegistration).Assembly;

    [Fact]
    public void Domain_must_not_depend_on_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Domain must not depend on Application"));
    }

    [Fact]
    public void Domain_must_not_depend_on_Adapters()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Adapters")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Domain must not depend on Adapters"));
    }

    [Fact]
    public void Domain_must_not_depend_on_Host()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Host")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Domain must not depend on Host"));
    }

    [Fact]
    public void Application_must_not_depend_on_Adapters()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Adapters")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Application must not depend on Adapters"));
    }

    [Fact]
    public void Application_must_not_depend_on_Host()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Host")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Application must not depend on Host"));
    }

    [Fact]
    public void Adapters_must_not_depend_on_Host()
    {
        var result = Types.InAssembly(AdaptersAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Host")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Adapters must not depend on Host"));
    }

    [Fact]
    public void Domain_must_not_depend_on_Microsoft_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Domain must not depend on ASP.NET Core"));
    }

    [Fact]
    public void Domain_must_not_depend_on_Microsoft_Extensions_Http()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Extensions.Http")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Domain must not depend on Microsoft.Extensions.Http"));
    }

    [Fact]
    public void Application_must_not_depend_on_Microsoft_AspNetCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Application must not depend on ASP.NET Core"));
    }

    [Fact]
    public void Application_must_not_depend_on_System_Net_Http_Json()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.Net.Http")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Application must not depend on System.Net.Http"));
    }

    // --- Worker dependency rules: Worker is a host-level project ---

    [Fact]
    public void Domain_must_not_depend_on_Worker()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Domain must not depend on Worker"));
    }

    [Fact]
    public void Application_must_not_depend_on_Worker()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Application must not depend on Worker"));
    }

    [Fact]
    public void Adapters_must_not_depend_on_Worker()
    {
        var result = Types.InAssembly(AdaptersAssembly)
            .ShouldNot()
            .HaveDependencyOn("DevOpsSite.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailure(result, "Adapters must not depend on Worker"));
    }

    private static string FormatFailure(TestResult result, string rule)
    {
        if (result.IsSuccessful) return string.Empty;
        var violators = result.FailingTypeNames ?? Enumerable.Empty<string>();
        return $"{rule}. Violating types: {string.Join(", ", violators)}";
    }
}
