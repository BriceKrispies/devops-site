using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;
using DevOpsSite.Domain.Entities;
using DevOpsSite.Domain.ValueObjects;

namespace DevOpsSite.Application.UseCases;

/// <summary>
/// Query: Get normalized health status for a known service.
/// Constitution §5: Bounded slice with contract, invariants, failure modes, observability.
/// </summary>
public sealed class GetServiceHealthHandler
{
    private readonly IServiceHealthPort _healthPort;
    private readonly ITelemetryPort _telemetry;
    private readonly IAuthorizationService _authz;

    public const string OperationName = "GetServiceHealth";

    /// <summary>Authorization metadata for this capability.</summary>
    public static readonly CapabilityDescriptor Descriptor = new()
    {
        OperationName = OperationName,
        RequiresAuthentication = true,
        RequiredPermissions = [Permission.WellKnown.ServiceHealthRead],
        IsPrivileged = false,
        RequiresAudit = false,
        Description = "Retrieve normalized health status for a service.",
        Category = CapabilityCategory.ServiceHealth,
        RiskLevel = RiskLevel.Low,
        ExecutionMode = ExecutionMode.Synchronous,
        Status = ImplementationStatus.Ready,
        ExecutionProfile = ExecutionProfile.Default
    };

    public GetServiceHealthHandler(IServiceHealthPort healthPort, ITelemetryPort telemetry, IAuthorizationService authz)
    {
        _healthPort = healthPort ?? throw new ArgumentNullException(nameof(healthPort));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _authz = authz ?? throw new ArgumentNullException(nameof(authz));
    }

    public async Task<Result<ServiceHealthSummary>> HandleAsync(GetServiceHealthQuery query, OperationContext ctx, CancellationToken ct = default)
    {
        var contextErrors = ctx.Validate();
        if (contextErrors.Count > 0)
            return Result<ServiceHealthSummary>.Failure(
                AppError.InvariantViolation(
                    $"Invalid operation context: {string.Join("; ", contextErrors)}",
                    OperationName,
                    ctx.CorrelationId ?? "unknown"));

        // Authorization — deny by default
        var authResult = _authz.Evaluate(OperationName, ctx);
        if (!authResult.IsAllowed)
        {
            return authResult.FailureReason == AuthorizationFailureReason.Unauthenticated
                ? Result<ServiceHealthSummary>.Failure(
                    AppError.Unauthenticated(authResult.Message!, OperationName, ctx.CorrelationId))
                : Result<ServiceHealthSummary>.Failure(
                    AppError.Forbidden(authResult.Message!, OperationName, ctx.CorrelationId));
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(query.ServiceId))
            return Result<ServiceHealthSummary>.Failure(
                AppError.Validation("ServiceId is required.", OperationName, ctx.CorrelationId));

        ServiceId serviceId;
        try
        {
            serviceId = ServiceId.Create(query.ServiceId);
        }
        catch (ArgumentException ex)
        {
            return Result<ServiceHealthSummary>.Failure(
                AppError.Validation(ex.Message, OperationName, ctx.CorrelationId));
        }

        // Telemetry: start span
        using var span = _telemetry.StartSpan(OperationName, ctx.CorrelationId);

        var result = await _healthPort.GetHealthAsync(serviceId, ctx, ct);

        // Telemetry: record result
        if (result.IsSuccess)
        {
            span.SetResult("success");
            _telemetry.IncrementCounter("capability.invocations", new Dictionary<string, string>
            {
                ["operationName"] = OperationName,
                ["result"] = "success"
            });
            _telemetry.LogInfo(OperationName, ctx.CorrelationId,
                $"Service health retrieved for {serviceId.Value}",
                new Dictionary<string, object>
                {
                    ["serviceId"] = serviceId.Value,
                    ["status"] = result.Value.Status.ToString()
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
                result.Error.Message,
                result.Error.Code.ToString(),
                result.Error.Dependency);
        }

        return result;
    }
}

/// <summary>
/// Input contract for GetServiceHealth.
/// </summary>
public sealed record GetServiceHealthQuery
{
    public required string ServiceId { get; init; }
}
