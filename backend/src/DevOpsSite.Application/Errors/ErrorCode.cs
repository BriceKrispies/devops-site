namespace DevOpsSite.Application.Errors;

/// <summary>
/// Stable error codes from the error taxonomy.
/// Constitution §9.2: Each capability must classify errors into stable categories.
/// </summary>
public enum ErrorCode
{
    Validation,
    Authorization,
    NotFound,
    Conflict,
    RateLimited,
    DependencyUnavailable,
    Timeout,
    TransientFailure,
    PermanentFailure,
    InvariantViolation
}
