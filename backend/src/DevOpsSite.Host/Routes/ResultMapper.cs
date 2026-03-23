using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Results;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Maps AppError to HTTP responses using the standardized ApiErrorResponse contract.
/// Single source of truth for error-to-HTTP mapping.
/// Constitution §12: Host contains only transport mapping, no business logic.
/// Constitution §14: Authorization errors mapped to 401 (unauthenticated) or 403 (forbidden).
/// Kill switch errors mapped to 503 (service unavailable).
/// </summary>
public static class ResultMapper
{
    /// <summary>
    /// Marker message prefix used by AppError.Unauthenticated factory.
    /// Allows stable 401 vs 403 distinction without adding fields to AppError.
    /// </summary>
    private const string UnauthenticatedPrefix = "Authentication required";

    public static IResult ToHttpResult(AppError error)
    {
        var response = ToApiErrorResponse(error);
        var statusCode = ToStatusCode(error);
        return Results.Json(response, statusCode: statusCode);
    }

    public static ApiErrorResponse ToApiErrorResponse(AppError error) => new()
    {
        Code = ToErrorCode(error),
        Message = error.Message,
        CorrelationId = error.CorrelationId,
        FieldErrors = error.FieldErrors
    };

    public static int ToStatusCode(AppError error) => error.Code switch
    {
        ErrorCode.Validation => 400,
        ErrorCode.Authorization when error.Message.StartsWith(UnauthenticatedPrefix, StringComparison.Ordinal) => 401,
        ErrorCode.Authorization when error.Message.Contains("kill switch", StringComparison.OrdinalIgnoreCase) => 503,
        ErrorCode.Authorization => 403,
        ErrorCode.NotFound => 404,
        ErrorCode.Conflict => 409,
        ErrorCode.RateLimited => 429,
        _ => 502
    };

    private static string ToErrorCode(AppError error) => error.Code switch
    {
        ErrorCode.Authorization when error.Message.StartsWith(UnauthenticatedPrefix, StringComparison.Ordinal)
            => "UNAUTHENTICATED",
        ErrorCode.Authorization when error.Message.Contains("kill switch", StringComparison.OrdinalIgnoreCase)
            => "KILL_SWITCH_ACTIVE",
        ErrorCode.Authorization => "FORBIDDEN",
        _ => error.Code.ToString().ToUpperInvariant() switch
        {
            "NOTFOUND" => "NOT_FOUND",
            "DEPENDENCYUNAVAILABLE" => "DEPENDENCY_UNAVAILABLE",
            "TRANSIENTFAILURE" => "TRANSIENT_FAILURE",
            "PERMANENTFAILURE" => "PERMANENT_FAILURE",
            "INVARIANTVIOLATION" => "INTERNAL_ERROR",
            "RATELIMITED" => "RATE_LIMITED",
            var code => code
        }
    };

    /// <summary>
    /// Creates a sanitized ApiErrorResponse for unhandled exceptions.
    /// Never exposes internal exception details to the client.
    /// </summary>
    public static ApiErrorResponse InternalError(string correlationId) => new()
    {
        Code = "INTERNAL_ERROR",
        Message = "Something went wrong while processing your request.",
        CorrelationId = correlationId
    };
}
