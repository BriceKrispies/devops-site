using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Tests for the OperationalCapabilityCatalog and capability shell model.
/// Verifies that all capabilities (implemented and planned) are classified
/// correctly and that the catalog is consistent with handler descriptors.
/// </summary>
public sealed class CapabilityCatalogTests
{
    // ──────────────────────────────────────────────────────────────
    //  Catalog completeness
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Catalog_must_contain_all_implemented_handler_descriptors()
    {
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

            var catalogEntry = OperationalCapabilityCatalog.GetByOperationName(operationName);
            Assert.NotNull(catalogEntry);
            Assert.Equal(ImplementationStatus.Ready, catalogEntry.Status);
        }
    }

    [Fact]
    public void Catalog_All_must_include_both_implemented_and_planned()
    {
        var all = OperationalCapabilityCatalog.All;
        Assert.True(all.Count >= 10, $"Expected at least 10 catalog entries, got {all.Count}");

        var implemented = all.Where(c => c.Status == ImplementationStatus.Ready).ToList();
        var planned = all.Where(c => c.Status == ImplementationStatus.Planned).ToList();

        Assert.True(implemented.Count >= 5, "Expected at least 5 implemented capabilities");
        Assert.True(planned.Count >= 5, "Expected at least 5 planned capabilities");
    }

    [Fact]
    public void Catalog_Implemented_matches_Ready_status()
    {
        foreach (var entry in OperationalCapabilityCatalog.Implemented)
        {
            Assert.Equal(ImplementationStatus.Ready, entry.Status);
        }
    }

    [Fact]
    public void Catalog_Planned_matches_Planned_status()
    {
        foreach (var entry in OperationalCapabilityCatalog.Planned)
        {
            Assert.Equal(ImplementationStatus.Planned, entry.Status);
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Descriptor validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Every_catalog_entry_must_be_internally_valid()
    {
        foreach (var entry in OperationalCapabilityCatalog.All)
        {
            var errors = entry.Validate();
            Assert.True(errors.Count == 0,
                $"Catalog entry '{entry.OperationName}' has validation errors: {string.Join("; ", errors)}");
        }
    }

    [Fact]
    public void Every_catalog_entry_must_have_unique_operation_name()
    {
        var names = OperationalCapabilityCatalog.All.Select(c => c.OperationName).ToList();
        var duplicates = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Every_catalog_entry_must_have_a_description()
    {
        foreach (var entry in OperationalCapabilityCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Description),
                $"Catalog entry '{entry.OperationName}' is missing a description.");
        }
    }

    [Fact]
    public void Every_catalog_entry_must_require_authentication()
    {
        // Constitution §14: deny by default. All operational capabilities require auth.
        foreach (var entry in OperationalCapabilityCatalog.All)
        {
            Assert.True(entry.RequiresAuthentication,
                $"Catalog entry '{entry.OperationName}' must require authentication.");
        }
    }

    [Fact]
    public void Every_catalog_entry_must_have_at_least_one_permission()
    {
        foreach (var entry in OperationalCapabilityCatalog.All)
        {
            Assert.True(entry.RequiredPermissions.Count > 0,
                $"Catalog entry '{entry.OperationName}' must declare at least one required permission.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Risk classification rules
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void High_risk_capabilities_must_require_audit()
    {
        foreach (var entry in OperationalCapabilityCatalog.All.Where(c => c.RiskLevel >= RiskLevel.High))
        {
            Assert.True(entry.RequiresAudit,
                $"High/Critical risk capability '{entry.OperationName}' must require audit.");
        }
    }

    [Fact]
    public void High_risk_capabilities_must_be_privileged()
    {
        foreach (var entry in OperationalCapabilityCatalog.All.Where(c => c.RiskLevel >= RiskLevel.High))
        {
            Assert.True(entry.IsPrivileged,
                $"High/Critical risk capability '{entry.OperationName}' must be privileged.");
        }
    }

    [Fact]
    public void Low_risk_read_only_capabilities_must_not_be_privileged()
    {
        foreach (var entry in OperationalCapabilityCatalog.All
            .Where(c => c.RiskLevel == RiskLevel.Low && !c.IsPrivileged))
        {
            Assert.False(entry.RequiresAudit,
                $"Low-risk non-privileged capability '{entry.OperationName}' should not require audit.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Execution profile rules
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ReadOnly_profile_capabilities_must_be_low_risk()
    {
        foreach (var entry in OperationalCapabilityCatalog.All
            .Where(c => c.ExecutionProfile == ExecutionProfile.ReadOnly))
        {
            Assert.Equal(RiskLevel.Low, entry.RiskLevel);
        }
    }

    [Fact]
    public void Operator_profile_capabilities_must_be_privileged()
    {
        var operatorProfiles = new[]
        {
            ExecutionProfile.QueueOperator,
            ExecutionProfile.DatabaseOperator,
            ExecutionProfile.Admin
        };

        foreach (var entry in OperationalCapabilityCatalog.All
            .Where(c => operatorProfiles.Contains(c.ExecutionProfile)))
        {
            Assert.True(entry.IsPrivileged,
                $"Operator-profile capability '{entry.OperationName}' (profile={entry.ExecutionProfile}) must be privileged.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Reserved AWS capability verification
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("QueuesRead", CapabilityCategory.Queues, RiskLevel.Low, ExecutionMode.Synchronous, ExecutionProfile.ReadOnly)]
    [InlineData("QueuesRedriveDlq", CapabilityCategory.Queues, RiskLevel.High, ExecutionMode.Asynchronous, ExecutionProfile.QueueOperator)]
    [InlineData("DatabasesRead", CapabilityCategory.Databases, RiskLevel.Low, ExecutionMode.Synchronous, ExecutionProfile.ReadOnly)]
    [InlineData("DatabasesCloneNonProd", CapabilityCategory.Databases, RiskLevel.Critical, ExecutionMode.Asynchronous, ExecutionProfile.DatabaseOperator)]
    [InlineData("LogsRead", CapabilityCategory.Logs, RiskLevel.Low, ExecutionMode.Synchronous, ExecutionProfile.ReadOnly)]
    public void Reserved_AWS_capability_must_be_classified_correctly(
        string operationName, CapabilityCategory category, RiskLevel risk, ExecutionMode mode, ExecutionProfile profile)
    {
        var entry = OperationalCapabilityCatalog.GetByOperationName(operationName);

        Assert.NotNull(entry);
        Assert.Equal(ImplementationStatus.Planned, entry.Status);
        Assert.Equal(category, entry.Category);
        Assert.Equal(risk, entry.RiskLevel);
        Assert.Equal(mode, entry.ExecutionMode);
        Assert.Equal(profile, entry.ExecutionProfile);
    }

    // ──────────────────────────────────────────────────────────────
    //  Implemented capability wired through the model
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void QueryTraceEvents_handler_descriptor_matches_catalog()
    {
        var handlerDescriptor = QueryTraceEventsHandler.Descriptor;
        var catalogEntry = OperationalCapabilityCatalog.GetByOperationName("QueryTraceEvents");

        Assert.NotNull(catalogEntry);
        Assert.Equal(handlerDescriptor.OperationName, catalogEntry.OperationName);
        Assert.Equal(handlerDescriptor.Category, catalogEntry.Category);
        Assert.Equal(handlerDescriptor.RiskLevel, catalogEntry.RiskLevel);
        Assert.Equal(handlerDescriptor.ExecutionMode, catalogEntry.ExecutionMode);
        Assert.Equal(handlerDescriptor.ExecutionProfile, catalogEntry.ExecutionProfile);
        Assert.Equal(ImplementationStatus.Ready, handlerDescriptor.Status);
    }

    [Fact]
    public void All_implemented_handlers_have_matching_catalog_classification()
    {
        var handlers = new[]
        {
            (QueryTraceEventsHandler.OperationName, QueryTraceEventsHandler.Descriptor),
            (AddTraceEventsHandler.OperationName, AddTraceEventsHandler.Descriptor),
            (IngestTraceEventsHandler.OperationName, IngestTraceEventsHandler.Descriptor),
            (GetServiceHealthHandler.OperationName, GetServiceHealthHandler.Descriptor),
            (GetWorkItemHandler.OperationName, GetWorkItemHandler.Descriptor)
        };

        foreach (var (name, descriptor) in handlers)
        {
            var catalogEntry = OperationalCapabilityCatalog.GetByOperationName(name);
            Assert.NotNull(catalogEntry);

            Assert.Equal(descriptor.Category, catalogEntry.Category);
            Assert.Equal(descriptor.RiskLevel, catalogEntry.RiskLevel);
            Assert.Equal(descriptor.ExecutionMode, catalogEntry.ExecutionMode);
            Assert.Equal(descriptor.Status, catalogEntry.Status);
            Assert.Equal(descriptor.ExecutionProfile, catalogEntry.ExecutionProfile);
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Category queries
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ByCategory_returns_correct_capabilities()
    {
        var queues = OperationalCapabilityCatalog.ByCategory(CapabilityCategory.Queues);
        Assert.Equal(2, queues.Count);
        Assert.All(queues, c => Assert.Equal(CapabilityCategory.Queues, c.Category));

        var databases = OperationalCapabilityCatalog.ByCategory(CapabilityCategory.Databases);
        Assert.Equal(2, databases.Count);

        var logs = OperationalCapabilityCatalog.ByCategory(CapabilityCategory.Logs);
        Assert.Single(logs);
    }

    // ──────────────────────────────────────────────────────────────
    //  Descriptor validation rules for new fields
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Validation_rejects_high_risk_without_audit()
    {
        var descriptor = new CapabilityDescriptor
        {
            OperationName = "TestHighRiskNoAudit",
            RequiresAuthentication = true,
            RequiredPermissions = [Permission.WellKnown.QueuesOperate],
            IsPrivileged = true,
            RequiresAudit = false, // violation: high risk must require audit
            RiskLevel = RiskLevel.High
        };

        var errors = descriptor.Validate();
        Assert.Contains(errors, e => e.Contains("audit"));
    }

    [Fact]
    public void Validation_rejects_critical_risk_without_auth()
    {
        var descriptor = new CapabilityDescriptor
        {
            OperationName = "TestCriticalNoAuth",
            RequiresAuthentication = false,
            RiskLevel = RiskLevel.Critical
        };

        var errors = descriptor.Validate();
        Assert.Contains(errors, e => e.Contains("authentication"));
    }
}
