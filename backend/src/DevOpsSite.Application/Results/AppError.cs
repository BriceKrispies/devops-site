using DevOpsSite.Application.Errors;

namespace DevOpsSite.Application.Results;

/// <summary>
/// Typed, classified error. Constitution §9: Errors must be explicit, typed, and classified.
/// </summary>
public sealed record AppError
{
    public required ErrorCode Code { get; init; }
    public required string Message { get; init; }
    public required Severity Severity { get; init; }
    public required string OperationName { get; init; }
    public required string CorrelationId { get; init; }
    public string? Dependency { get; init; }
    public Exception? Cause { get; init; }
    public IReadOnlyDictionary<string, string>? FieldErrors { get; init; }

    public static AppError Unauthenticated(string message, string operationName, string correlationId) =>
        new()
        {
            Code = ErrorCode.Authorization,
            Message = message,
            Severity = Severity.Warn,
            OperationName = operationName,
            CorrelationId = correlationId
        };

    public static AppError Forbidden(string message, string operationName, string correlationId) =>
        new()
        {
            Code = ErrorCode.Authorization,
            Message = message,
            Severity = Severity.Warn,
            OperationName = operationName,
            CorrelationId = correlationId
        };

    public static AppError Validation(string message, string operationName, string correlationId) =>
        new()
        {
            Code = ErrorCode.Validation,
            Message = message,
            Severity = Severity.Warn,
            OperationName = operationName,
            CorrelationId = correlationId
        };

    public static AppError NotFound(string message, string operationName, string correlationId) =>
        new()
        {
            Code = ErrorCode.NotFound,
            Message = message,
            Severity = Severity.Info,
            OperationName = operationName,
            CorrelationId = correlationId
        };

    public static AppError DependencyUnavailable(string message, string operationName, string correlationId, string dependency, Exception? cause = null) =>
        new()
        {
            Code = ErrorCode.DependencyUnavailable,
            Message = message,
            Severity = Severity.Error,
            OperationName = operationName,
            CorrelationId = correlationId,
            Dependency = dependency,
            Cause = cause
        };

    public static AppError Timeout(string message, string operationName, string correlationId, string dependency, Exception? cause = null) =>
        new()
        {
            Code = ErrorCode.Timeout,
            Message = message,
            Severity = Severity.Error,
            OperationName = operationName,
            CorrelationId = correlationId,
            Dependency = dependency,
            Cause = cause
        };

    public static AppError TransientFailure(string message, string operationName, string correlationId, string dependency, Exception? cause = null) =>
        new()
        {
            Code = ErrorCode.TransientFailure,
            Message = message,
            Severity = Severity.Error,
            OperationName = operationName,
            CorrelationId = correlationId,
            Dependency = dependency,
            Cause = cause
        };

    public static AppError PermanentFailure(string message, string operationName, string correlationId, string dependency, Exception? cause = null) =>
        new()
        {
            Code = ErrorCode.PermanentFailure,
            Message = message,
            Severity = Severity.Error,
            OperationName = operationName,
            CorrelationId = correlationId,
            Dependency = dependency,
            Cause = cause
        };

    public static AppError InvariantViolation(string message, string operationName, string correlationId) =>
        new()
        {
            Code = ErrorCode.InvariantViolation,
            Message = message,
            Severity = Severity.Fatal,
            OperationName = operationName,
            CorrelationId = correlationId
        };

    public static AppError Conflict(string message, string operationName, string correlationId) =>
        new()
        {
            Code = ErrorCode.Conflict,
            Message = message,
            Severity = Severity.Warn,
            OperationName = operationName,
            CorrelationId = correlationId
        };

    public static AppError ValidationWithFields(string message, string operationName, string correlationId, IReadOnlyDictionary<string, string> fieldErrors) =>
        new()
        {
            Code = ErrorCode.Validation,
            Message = message,
            Severity = Severity.Warn,
            OperationName = operationName,
            CorrelationId = correlationId,
            FieldErrors = fieldErrors
        };
}
