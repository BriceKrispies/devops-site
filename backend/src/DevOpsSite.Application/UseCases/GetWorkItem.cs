using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.UseCases;

public sealed record GetWorkItemQuery
{
    public required string Key { get; init; }
}

public sealed class GetWorkItemHandler
{
    private readonly IWorkItemPort _workItemPort;
    private readonly ITelemetryPort _telemetry;
    private readonly IAuthorizationService _authz;

    public const string OperationName = "GetWorkItem";

    /// <summary>Authorization metadata for this capability.</summary>
    public static readonly CapabilityDescriptor Descriptor = new()
    {
        OperationName = OperationName,
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.WorkItemRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Retrieve a normalized work item summary by key.",
        Category = CapabilityCategory.WorkItems,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    public GetWorkItemHandler(IWorkItemPort workItemPort, ITelemetryPort telemetry, IAuthorizationService authz)
    {
        _workItemPort = workItemPort ?? throw new ArgumentNullException(nameof(workItemPort));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _authz = authz ?? throw new ArgumentNullException(nameof(authz));
    }

    public async Task<Result<WorkItemSummary>> HandleAsync(GetWorkItemQuery query, OperationContext ctx, CancellationToken ct = default)
    {
        var contextErrors = ctx.Validate();
        if (contextErrors.Count > 0)
            return Result<WorkItemSummary>.Failure(
                AppError.InvariantViolation(
                    $"Invalid operation context: {string.Join("; ", contextErrors)}",
                    OperationName, ctx.CorrelationId ?? "unknown"));

        // Authorization — deny by default
        var authResult = _authz.Evaluate(OperationName, ctx);
        if (!authResult.IsAllowed)
        {
            return authResult.FailureReason == AuthorizationFailureReason.Unauthenticated
                ? Result<WorkItemSummary>.Failure(
                    AppError.Unauthenticated(authResult.Message!, OperationName, ctx.CorrelationId))
                : Result<WorkItemSummary>.Failure(
                    AppError.Forbidden(authResult.Message!, OperationName, ctx.CorrelationId));
        }

        if (string.IsNullOrWhiteSpace(query.Key))
            return Result<WorkItemSummary>.Failure(
                AppError.Validation("Work item key is required.", OperationName, ctx.CorrelationId));

        WorkItemKey workItemKey;
        try
        {
            workItemKey = WorkItemKey.Create(query.Key);
        }
        catch (ArgumentException ex)
        {
            return Result<WorkItemSummary>.Failure(
                AppError.Validation(ex.Message, OperationName, ctx.CorrelationId));
        }

        using var span = _telemetry.StartSpan(OperationName, ctx.CorrelationId);

        var result = await _workItemPort.GetByKeyAsync(workItemKey, ctx, ct);

        if (result.IsSuccess)
        {
            span.SetResult("success");
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "success"
            });
            _telemetry.LogInfo(OperationName, ctx.CorrelationId,
                $"Work item retrieved: {workItemKey.Value}",
                new Dictionary<string, object>
                {
                    ["workItemKey"] = workItemKey.Value,
                    ["status"] = result.Value.Status
                });
        }
        else
        {
            span.SetError(result.Error.Code.ToString(), result.Error.Message);
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "failure"
            });
            _telemetry.LogError(OperationName, ctx.CorrelationId,
                result.Error.Message, result.Error.Code.ToString(), result.Error.Dependency);
        }

        return result;
    }
}
