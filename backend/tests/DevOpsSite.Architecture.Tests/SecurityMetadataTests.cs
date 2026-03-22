using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Constitution §14: Deny by default security validation.
/// These tests verify that every capability has authorization metadata
/// and that the security model invariants hold.
/// </summary>
public sealed class SecurityMetadataTests
{
    /// <summary>
    /// All known handler types and their expected descriptors.
    /// When a new handler is added, it MUST be added here — otherwise this test fails.
    /// </summary>
    private static readonly Dictionary<string, CapabilityDescriptor> AllDescriptors = new()
    {
        [GetServiceHealthHandler.OperationName] = GetServiceHealthHandler.Descriptor,
        [GetWorkItemHandler.OperationName] = GetWorkItemHandler.Descriptor,
        [AddTraceEventsHandler.OperationName] = AddTraceEventsHandler.Descriptor,
        [QueryTraceEventsHandler.OperationName] = QueryTraceEventsHandler.Descriptor,
        [IngestTraceEventsHandler.OperationName] = IngestTraceEventsHandler.Descriptor
    };

    [Fact]
    public void Every_handler_must_have_a_capability_descriptor()
    {
        // Enumerate all handler types in the Application assembly
        var handlerTypes = typeof(Application.Results.Result<>).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Handler") && !t.IsAbstract && !t.IsInterface)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var operationNameField = handlerType.GetField("OperationName",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.NotNull(operationNameField);
            var operationName = (string)operationNameField.GetValue(null)!;

            Assert.True(AllDescriptors.ContainsKey(operationName),
                $"Handler '{handlerType.Name}' (OperationName='{operationName}') is missing from SecurityMetadataTests.AllDescriptors. " +
                "Every handler must declare and register a CapabilityDescriptor.");
        }
    }

    [Fact]
    public void Every_descriptor_must_be_internally_valid()
    {
        foreach (var (name, descriptor) in AllDescriptors)
        {
            var errors = descriptor.Validate();
            Assert.True(errors.Count == 0,
                $"Descriptor for '{name}' has validation errors: {string.Join("; ", errors)}");
        }
    }

    [Fact]
    public void Every_handler_must_have_a_static_descriptor_field()
    {
        var handlerTypes = typeof(Application.Results.Result<>).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Handler") && !t.IsAbstract && !t.IsInterface)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var descriptorField = handlerType.GetField("Descriptor",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.NotNull(descriptorField);
            Assert.Equal(typeof(CapabilityDescriptor), descriptorField.FieldType);

            var descriptor = (CapabilityDescriptor)descriptorField.GetValue(null)!;
            Assert.NotNull(descriptor);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.OperationName));
        }
    }

    [Fact]
    public void Registry_must_contain_all_known_handlers()
    {
        var registry = new CapabilityRegistry();
        foreach (var descriptor in AllDescriptors.Values)
            registry.Register(descriptor);

        var handlerTypes = typeof(Application.Results.Result<>).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Handler") && !t.IsAbstract && !t.IsInterface)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var operationNameField = handlerType.GetField("OperationName",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var operationName = (string)operationNameField!.GetValue(null)!;
            var descriptor = registry.GetDescriptor(operationName);
            Assert.NotNull(descriptor);
        }
    }

    [Fact]
    public void Privileged_descriptors_must_require_audit()
    {
        foreach (var (name, descriptor) in AllDescriptors)
        {
            if (descriptor.IsPrivileged)
            {
                Assert.True(descriptor.RequiresAudit,
                    $"Privileged capability '{name}' must require audit.");
                Assert.True(descriptor.RequiresAuthentication,
                    $"Privileged capability '{name}' must require authentication.");
            }
        }
    }

    [Fact]
    public void Public_descriptors_must_not_require_permissions()
    {
        foreach (var (name, descriptor) in AllDescriptors)
        {
            if (!descriptor.RequiresAuthentication)
            {
                Assert.Empty(descriptor.RequiredPermissions);
            }
        }
    }

    [Fact]
    public void Unregistered_capability_must_be_denied_by_default()
    {
        var registry = new CapabilityRegistry();
        // Do NOT register anything
        var authz = new AuthorizationService(registry);

        var ctx = new Application.Context.OperationContext
        {
            CorrelationId = "test",
            OperationName = "UnknownOperation",
            Timestamp = DateTimeOffset.UtcNow,
            Source = Application.Context.OperationSource.HttpRequest,
            Actor = new Application.Context.ActorIdentity
            {
                Id = "admin",
                Type = Application.Context.ActorType.User
            }
        };

        var result = authz.Evaluate("UnknownOperation", ctx);

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.MissingDescriptor, result.FailureReason);
    }

    [Fact]
    public void Authenticated_user_without_permission_must_be_denied()
    {
        var registry = new CapabilityRegistry();
        registry.Register(GetWorkItemHandler.Descriptor);
        var authz = new AuthorizationService(registry);

        var ctx = new Application.Context.OperationContext
        {
            CorrelationId = "test",
            OperationName = GetWorkItemHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = Application.Context.OperationSource.HttpRequest,
            Actor = new Application.Context.ActorIdentity
            {
                Id = "user-no-perms",
                Type = Application.Context.ActorType.User
            },
            Permissions = new HashSet<Permission>()
        };

        var result = authz.Evaluate(GetWorkItemHandler.OperationName, ctx);

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.Forbidden, result.FailureReason);
    }

    [Fact]
    public void Unauthenticated_request_must_be_denied_for_protected_capability()
    {
        var registry = new CapabilityRegistry();
        registry.Register(GetWorkItemHandler.Descriptor);
        var authz = new AuthorizationService(registry);

        var ctx = new Application.Context.OperationContext
        {
            CorrelationId = "test",
            OperationName = GetWorkItemHandler.OperationName,
            Timestamp = DateTimeOffset.UtcNow,
            Source = Application.Context.OperationSource.HttpRequest
            // No Actor
        };

        var result = authz.Evaluate(GetWorkItemHandler.OperationName, ctx);

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.Unauthenticated, result.FailureReason);
    }
}
