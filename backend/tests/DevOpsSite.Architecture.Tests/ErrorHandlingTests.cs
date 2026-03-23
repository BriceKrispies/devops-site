using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Results;
using DevOpsSite.Host.Routes;

namespace DevOpsSite.Architecture.Tests;

/// <summary>
/// Tests for the standardized error handling pipeline:
/// - ApiErrorResponse contract shape
/// - ResultMapper error-to-HTTP mapping
/// - Correlation ID inclusion in all error responses
/// - Field error propagation for validation failures
/// - Exception-safe InternalError factory
/// </summary>
public sealed class ErrorHandlingTests
{
    private const string TestCorrelationId = "test-corr-abc123";
    private const string TestOperation = "TestOperation";

    // --- ApiErrorResponse contract ---

    [Fact]
    public void ApiErrorResponse_includes_all_required_fields()
    {
        var response = new ApiErrorResponse
        {
            Code = "VALIDATION",
            Message = "Invalid input.",
            CorrelationId = TestCorrelationId
        };

        Assert.Equal("VALIDATION", response.Code);
        Assert.Equal("Invalid input.", response.Message);
        Assert.Equal(TestCorrelationId, response.CorrelationId);
        Assert.Null(response.Details);
        Assert.Null(response.FieldErrors);
    }

    [Fact]
    public void ApiErrorResponse_includes_field_errors_when_present()
    {
        var fieldErrors = new Dictionary<string, string> { ["email"] = "Required" };
        var response = new ApiErrorResponse
        {
            Code = "VALIDATION",
            Message = "Validation failed.",
            CorrelationId = TestCorrelationId,
            FieldErrors = fieldErrors
        };

        Assert.NotNull(response.FieldErrors);
        Assert.Equal("Required", response.FieldErrors["email"]);
    }

    // --- ResultMapper: error code mapping ---

    [Fact]
    public void Validation_error_maps_to_400()
    {
        var error = AppError.Validation("Bad input", TestOperation, TestCorrelationId);
        Assert.Equal(400, ResultMapper.ToStatusCode(error));
    }

    [Fact]
    public void Unauthenticated_error_maps_to_401()
    {
        var error = AppError.Unauthenticated("Authentication required", TestOperation, TestCorrelationId);
        Assert.Equal(401, ResultMapper.ToStatusCode(error));
    }

    [Fact]
    public void Forbidden_error_maps_to_403()
    {
        var error = AppError.Forbidden("Missing required permission: x", TestOperation, TestCorrelationId);
        Assert.Equal(403, ResultMapper.ToStatusCode(error));
    }

    [Fact]
    public void NotFound_error_maps_to_404()
    {
        var error = AppError.NotFound("Not found", TestOperation, TestCorrelationId);
        Assert.Equal(404, ResultMapper.ToStatusCode(error));
    }

    [Fact]
    public void Conflict_error_maps_to_409()
    {
        var error = AppError.Conflict("Already exists", TestOperation, TestCorrelationId);
        Assert.Equal(409, ResultMapper.ToStatusCode(error));
    }

    [Fact]
    public void DependencyUnavailable_error_maps_to_502()
    {
        var error = AppError.DependencyUnavailable("Service down", TestOperation, TestCorrelationId, "ext-svc");
        Assert.Equal(502, ResultMapper.ToStatusCode(error));
    }

    [Fact]
    public void Timeout_error_maps_to_502()
    {
        var error = AppError.Timeout("Timed out", TestOperation, TestCorrelationId, "ext-svc");
        Assert.Equal(502, ResultMapper.ToStatusCode(error));
    }

    // --- ResultMapper: standardized response shape ---

    [Fact]
    public void ToApiErrorResponse_always_includes_correlationId()
    {
        var error = AppError.NotFound("Not found", TestOperation, TestCorrelationId);
        var response = ResultMapper.ToApiErrorResponse(error);

        Assert.Equal(TestCorrelationId, response.CorrelationId);
        Assert.Equal("NOT_FOUND", response.Code);
        Assert.Equal("Not found", response.Message);
    }

    [Fact]
    public void ToApiErrorResponse_maps_validation_code()
    {
        var error = AppError.Validation("Bad", TestOperation, TestCorrelationId);
        var response = ResultMapper.ToApiErrorResponse(error);
        Assert.Equal("VALIDATION", response.Code);
    }

    [Fact]
    public void ToApiErrorResponse_maps_unauthenticated_code()
    {
        var error = AppError.Unauthenticated("Authentication required", TestOperation, TestCorrelationId);
        var response = ResultMapper.ToApiErrorResponse(error);
        Assert.Equal("UNAUTHENTICATED", response.Code);
    }

    [Fact]
    public void ToApiErrorResponse_maps_forbidden_code()
    {
        var error = AppError.Forbidden("Denied", TestOperation, TestCorrelationId);
        var response = ResultMapper.ToApiErrorResponse(error);
        Assert.Equal("FORBIDDEN", response.Code);
    }

    [Fact]
    public void ToApiErrorResponse_maps_dependency_unavailable_code()
    {
        var error = AppError.DependencyUnavailable("Down", TestOperation, TestCorrelationId, "svc");
        var response = ResultMapper.ToApiErrorResponse(error);
        Assert.Equal("DEPENDENCY_UNAVAILABLE", response.Code);
    }

    [Fact]
    public void ToApiErrorResponse_maps_invariant_violation_to_internal_error()
    {
        var error = AppError.InvariantViolation("Bug", TestOperation, TestCorrelationId);
        var response = ResultMapper.ToApiErrorResponse(error);
        Assert.Equal("INTERNAL_ERROR", response.Code);
    }

    [Fact]
    public void ToApiErrorResponse_propagates_field_errors()
    {
        var fields = new Dictionary<string, string> { ["name"] = "Too short" };
        var error = AppError.ValidationWithFields("Validation failed", TestOperation, TestCorrelationId, fields);
        var response = ResultMapper.ToApiErrorResponse(error);

        Assert.NotNull(response.FieldErrors);
        Assert.Equal("Too short", response.FieldErrors["name"]);
    }

    // --- InternalError factory ---

    [Fact]
    public void InternalError_returns_safe_generic_message()
    {
        var response = ResultMapper.InternalError(TestCorrelationId);

        Assert.Equal("INTERNAL_ERROR", response.Code);
        Assert.Equal(TestCorrelationId, response.CorrelationId);
        Assert.DoesNotContain("exception", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- AppError factory methods ---

    [Fact]
    public void Conflict_factory_creates_correct_error()
    {
        var error = AppError.Conflict("Already exists", TestOperation, TestCorrelationId);
        Assert.Equal(ErrorCode.Conflict, error.Code);
        Assert.Equal(Severity.Warn, error.Severity);
    }

    [Fact]
    public void ValidationWithFields_factory_carries_field_errors()
    {
        var fields = new Dictionary<string, string> { ["email"] = "Invalid format" };
        var error = AppError.ValidationWithFields("Invalid", TestOperation, TestCorrelationId, fields);
        Assert.Equal(ErrorCode.Validation, error.Code);
        Assert.NotNull(error.FieldErrors);
        Assert.Equal("Invalid format", error.FieldErrors["email"]);
    }

    // --- Kill switch mapping ---

    [Fact]
    public void Kill_switch_error_maps_to_503()
    {
        var error = AppError.Forbidden("Capability disabled via kill switch", TestOperation, TestCorrelationId);
        Assert.Equal(503, ResultMapper.ToStatusCode(error));
        var response = ResultMapper.ToApiErrorResponse(error);
        Assert.Equal("KILL_SWITCH_ACTIVE", response.Code);
    }
}
