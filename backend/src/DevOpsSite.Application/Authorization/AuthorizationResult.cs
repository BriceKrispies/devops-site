namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Outcome of an authorization evaluation. Stable internal type.
/// </summary>
public sealed record AuthorizationResult
{
    public bool IsAllowed { get; }
    public AuthorizationFailureReason? FailureReason { get; }
    public string? Message { get; }

    private AuthorizationResult(bool isAllowed, AuthorizationFailureReason? reason, string? message)
    {
        IsAllowed = isAllowed;
        FailureReason = reason;
        Message = message;
    }

    public static AuthorizationResult Allowed() => new(true, null, null);

    public static AuthorizationResult Unauthenticated(string? message = null) =>
        new(false, AuthorizationFailureReason.Unauthenticated, message ?? "Authentication required.");

    public static AuthorizationResult Forbidden(string? message = null) =>
        new(false, AuthorizationFailureReason.Forbidden, message ?? "Insufficient permissions.");

    public static AuthorizationResult MissingDescriptor(string operationName) =>
        new(false, AuthorizationFailureReason.MissingDescriptor,
            $"No authorization descriptor registered for capability '{operationName}'. Deny by default.");

    public static AuthorizationResult KillSwitched(string operationName, string? reason = null) =>
        new(false, AuthorizationFailureReason.KillSwitchActive,
            reason ?? $"Capability '{operationName}' has been emergency-disabled by kill switch.");
}

public enum AuthorizationFailureReason
{
    Unauthenticated,
    Forbidden,
    MissingDescriptor,
    KillSwitchActive
}
