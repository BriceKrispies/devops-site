using NetArchTest.Rules;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Constitution §3, §12: Layer content rules.
/// Ports must be interfaces in Application.
/// Host must not contain domain entities.
/// </summary>
public sealed class LayerContentTests
{
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.Results.Result<>).Assembly;

    [Fact]
    public void Port_interfaces_should_reside_in_Application_Ports_namespace()
    {
        var portTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("DevOpsSite.Application.Ports")
            .GetTypes();

        foreach (var type in portTypes)
        {
            Assert.True(type.IsInterface, $"Port type {type.FullName} must be an interface.");
        }
    }

    [Fact]
    public void Use_case_handlers_should_reside_in_Application_UseCases_namespace()
    {
        var handlerTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        foreach (var type in handlerTypes)
        {
            Assert.StartsWith("DevOpsSite.Application.UseCases", type.Namespace ?? "");
        }
    }
}
