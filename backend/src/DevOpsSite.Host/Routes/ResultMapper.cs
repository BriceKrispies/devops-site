using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Results;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Maps AppError to HTTP responses. Single source of truth for error-to-HTTP mapping.
/// Constitution §12: Host contains only transport mapping, no business logic.
/// Constitution §14: Authorization errors mapped to 401 (unauthenticated) or 403 (forbidden).
/// </summary>
public static class ResultMapper
{
    /// <summary>
    /// Marker message prefix used by AppError.Unauthenticated factory.
    /// Allows stable 401 vs 403 distinction without adding fields to AppError.
    /// </summary>
    private const string UnauthenticatedPrefix = "Authentication required";

    public static IResult ToHttpResult(AppError error) => error.Code switch
    {
        ErrorCode.Validation => Results.BadRequest(
            new { error = error.Code.ToString(), message = error.Message }),

        ErrorCode.Authorization when error.Message.StartsWith(UnauthenticatedPrefix, StringComparison.Ordinal) =>
            Results.Json(new { error = error.Code.ToString(), message = error.Message }, statusCode: 401),

        ErrorCode.Authorization => Results.Json(
            new { error = error.Code.ToString(), message = error.Message }, statusCode: 403),

        ErrorCode.NotFound => Results.NotFound(
            new { error = error.Code.ToString(), message = error.Message }),

        _ => Results.Problem(
            detail: error.Message,
            statusCode: 502,
            title: error.Code.ToString())
    };
}
