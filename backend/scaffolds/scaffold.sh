#!/usr/bin/env bash
# Backend Scaffold Generator
# Constitution §13.3: New capabilities must be created through approved scaffolds.
#
# Usage:
#   ./scaffolds/scaffold.sh capability <Name>
#   ./scaffolds/scaffold.sh port <Name>
#   ./scaffolds/scaffold.sh adapter <Name> <PortName>
#   ./scaffolds/scaffold.sh endpoint <Name> <Path>
#   ./scaffolds/scaffold.sh regression <BugId>

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

if [ $# -lt 2 ]; then
    echo "Usage: ./scaffolds/scaffold.sh <type> <name> [extra args]"
    echo "Types: capability, port, adapter, endpoint, regression"
    exit 1
fi

TYPE="$1"
NAME="$2"

case "$TYPE" in

    capability)
        echo "Scaffolding capability: $NAME"

        # Application use case
        mkdir -p "$ROOT_DIR/src/DevOpsSite.Application/UseCases"
        cat > "$ROOT_DIR/src/DevOpsSite.Application/UseCases/${NAME}.cs" << CSHARP
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.Ports;
using DevOpsSite.Application.Results;

namespace DevOpsSite.Application.UseCases;

public sealed record ${NAME}Command
{
    // Define input contract fields here
}

public sealed record ${NAME}Output
{
    // Define output contract fields here
}

public sealed class ${NAME}Handler
{
    private readonly ITelemetryPort _telemetry;
    private readonly IAuthorizationService _authz;

    public const string OperationName = "${NAME}";

    public static readonly CapabilityDescriptor Descriptor = new()
    {
        OperationName = OperationName,
        RequiresAuthentication = true,
        RequiredPermissions = [],          // TODO: add required permission
        IsPrivileged = false,              // TODO: set true if mutating
        RequiresAudit = false,             // TODO: set true if privileged
        Description = "TODO: describe this capability.",
        Category = CapabilityCategory.Admin,       // TODO: set correct category
        RiskLevel = RiskLevel.Low,                 // TODO: classify risk
        ExecutionMode = ExecutionMode.Synchronous, // TODO: sync or async
        Status = ImplementationStatus.Stub,        // Change to Ready when implemented
        ExecutionProfile = ExecutionProfile.Default // TODO: set execution profile
    };

    public ${NAME}Handler(ITelemetryPort telemetry, IAuthorizationService authz)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _authz = authz ?? throw new ArgumentNullException(nameof(authz));
    }

    public async Task<Result<${NAME}Output>> HandleAsync(${NAME}Command command, OperationContext ctx, CancellationToken ct = default)
    {
        var contextErrors = ctx.Validate();
        if (contextErrors.Count > 0)
            return Result<${NAME}Output>.Failure(
                AppError.InvariantViolation(
                    string.Join("; ", contextErrors), OperationName, ctx.CorrelationId ?? "unknown"));

        // Authorization — Constitution §14
        var authResult = _authz.Evaluate(OperationName, ctx);
        if (!authResult.IsAllowed)
        {
            return authResult.FailureReason == AuthorizationFailureReason.Unauthenticated
                ? Result<${NAME}Output>.Failure(AppError.Unauthenticated(authResult.Message!, OperationName, ctx.CorrelationId))
                : Result<${NAME}Output>.Failure(AppError.Forbidden(authResult.Message!, OperationName, ctx.CorrelationId));
        }

        using var span = _telemetry.StartSpan(OperationName, ctx.CorrelationId);

        // TODO: Validate input
        // TODO: Execute domain logic via ports
        // TODO: Record telemetry

        throw new NotImplementedException("Implement ${NAME} use case.");
    }
}
CSHARP

        # Application behavior test
        mkdir -p "$ROOT_DIR/tests/DevOpsSite.Application.Tests/UseCases"
        cat > "$ROOT_DIR/tests/DevOpsSite.Application.Tests/UseCases/${NAME}Tests.cs" << CSHARP
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;
using DevOpsSite.Application.UseCases;

namespace DevOpsSite.Application.Tests.UseCases;

/// <summary>
/// Application behavior tests for ${NAME}.
/// Constitution §6.2B.
/// </summary>
public sealed class ${NAME}Tests
{
    private readonly InMemoryTelemetryAdapter _telemetry = new();

    private static OperationContext CreateContext() => new()
    {
        CorrelationId = "test-corr-001",
        OperationName = ${NAME}Handler.OperationName,
        Timestamp = DateTimeOffset.UtcNow,
        Source = OperationSource.HttpRequest
    };

    [Fact]
    public async Task Should_succeed_when_input_is_valid()
    {
        // Arrange
        // Act
        // Assert
        throw new NotImplementedException("Write spec test first.");
    }

    [Fact]
    public async Task Should_return_validation_error_for_invalid_input()
    {
        throw new NotImplementedException("Write spec test first.");
    }

    // --- Observability assertions (Constitution §7.6) ---

    [Fact]
    public async Task Should_emit_span_on_execution()
    {
        throw new NotImplementedException("Write observability test.");
    }

    [Fact]
    public async Task Should_increment_counter_on_execution()
    {
        throw new NotImplementedException("Write observability test.");
    }
}
CSHARP

        echo "Created:"
        echo "  src/DevOpsSite.Application/UseCases/${NAME}.cs"
        echo "  tests/DevOpsSite.Application.Tests/UseCases/${NAME}Tests.cs"
        echo ""
        echo "Next steps:"
        echo "  1. Define input/output contracts"
        echo "  2. Add domain entities/services if needed"
        echo "  3. Define ports if external systems are involved (use: ./scaffolds/scaffold.sh port <PortName>)"
        echo "  4. Implement spec tests FIRST"
        echo "  5. Implement handler"
        echo "  6. Add observability assertions"
        echo "  7. Run: dotnet test"
        ;;

    port)
        echo "Scaffolding port: $NAME"

        # Port interface
        mkdir -p "$ROOT_DIR/src/DevOpsSite.Application/Ports"
        cat > "$ROOT_DIR/src/DevOpsSite.Application/Ports/I${NAME}.cs" << CSHARP
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Results;

namespace DevOpsSite.Application.Ports;

/// <summary>
/// Port for ${NAME}. Constitution §10.
/// </summary>
public interface I${NAME}
{
    // Define port methods here. Every method must:
    // - Accept OperationContext
    // - Return Result<T> (never throw for business flow)
    // - Accept CancellationToken
}
CSHARP

        # Contract test suite
        mkdir -p "$ROOT_DIR/tests/DevOpsSite.Contracts.Tests/${NAME}"
        cat > "$ROOT_DIR/tests/DevOpsSite.Contracts.Tests/${NAME}/${NAME}ContractTests.cs" << CSHARP
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Contracts.Tests.${NAME};

/// <summary>
/// Port contract test suite for I${NAME}.
/// Constitution §6.2C: Reusable suite that any adapter must pass.
/// </summary>
public abstract class ${NAME}ContractTests
{
    protected abstract I${NAME} CreateAdapter();

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "contract-test-001",
        OperationName = "ContractTest",
        Timestamp = DateTimeOffset.UtcNow,
        Source = OperationSource.HttpRequest
    };

    [Fact]
    public void Should_return_result_not_throw()
    {
        var adapter = CreateAdapter();
        Assert.NotNull(adapter);
        // Add contract tests for each port method
    }

    // Add: normal behavior, edge cases, failure behavior, timeout, telemetry tests
}
CSHARP

        echo "Created:"
        echo "  src/DevOpsSite.Application/Ports/I${NAME}.cs"
        echo "  tests/DevOpsSite.Contracts.Tests/${NAME}/${NAME}ContractTests.cs"
        echo ""
        echo "Next steps:"
        echo "  1. Define port methods"
        echo "  2. Write contract tests for each method"
        echo "  3. Create adapter: ./scaffolds/scaffold.sh adapter <AdapterName> ${NAME}"
        ;;

    adapter)
        if [ $# -lt 3 ]; then
            echo "Usage: ./scaffolds/scaffold.sh adapter <AdapterName> <PortName>"
            exit 1
        fi
        PORT_NAME="$3"
        echo "Scaffolding adapter: $NAME for port: $PORT_NAME"

        # Adapter implementation
        mkdir -p "$ROOT_DIR/src/DevOpsSite.Adapters/${NAME}"
        cat > "$ROOT_DIR/src/DevOpsSite.Adapters/${NAME}/${NAME}Adapter.cs" << CSHARP
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.${NAME};

/// <summary>
/// Adapter implementing I${PORT_NAME}.
/// Constitution §10: Maps vendor-specific behavior to port contract.
/// </summary>
public sealed class ${NAME}Adapter : I${PORT_NAME}
{
    private readonly ITelemetryPort _telemetry;

    public ${NAME}Adapter(ITelemetryPort telemetry)
    {
        _telemetry = telemetry;
    }

    // Implement port methods here.
    // - Map vendor errors to internal error taxonomy
    // - Emit telemetry spans for each external call
    // - Respect timeout and retry configuration
}
CSHARP

        # Adapter config
        cat > "$ROOT_DIR/src/DevOpsSite.Adapters/${NAME}/${NAME}Config.cs" << CSHARP
using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Adapters.${NAME};

/// <summary>
/// Typed configuration for ${NAME}. Constitution §11.
/// </summary>
public sealed class ${NAME}Config
{
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(1000, 60000)]
    public int TimeoutMs { get; set; } = 5000;
}
CSHARP

        # Adapter test (runs contract suite)
        mkdir -p "$ROOT_DIR/tests/DevOpsSite.Adapters.Tests/${NAME}"
        cat > "$ROOT_DIR/tests/DevOpsSite.Adapters.Tests/${NAME}/${NAME}AdapterTests.cs" << CSHARP
using DevOpsSite.Adapters.${NAME};
using DevOpsSite.Application.Ports;
using DevOpsSite.Contracts.Tests.${PORT_NAME};

namespace DevOpsSite.Adapters.Tests.${NAME};

/// <summary>
/// Adapter test for ${NAME}Adapter. Runs the port contract suite.
/// Constitution §6.2D.
/// </summary>
public sealed class ${NAME}AdapterTests : ${PORT_NAME}ContractTests
{
    protected override I${PORT_NAME} CreateAdapter()
    {
        // Create and return the adapter with test configuration
        throw new NotImplementedException("Configure adapter for testing.");
    }

    // Add adapter-specific tests:
    // - Serialization boundaries
    // - Vendor-specific error mapping
    // - Retry behavior
    // - Timeout behavior
}
CSHARP

        # Adapter certification tests (HTTP stub pattern)
        cat > "$ROOT_DIR/tests/DevOpsSite.Adapters.Tests/${NAME}/${NAME}CertificationTests.cs" << CSHARP
using System.Net;
using DevOpsSite.Adapters.${NAME};
using DevOpsSite.Adapters.Telemetry;
using DevOpsSite.Application.Context;
using DevOpsSite.Application.Errors;

namespace DevOpsSite.Adapters.Tests.${NAME};

/// <summary>
/// Certification tests for ${NAME}Adapter with controlled HTTP stubs.
/// Constitution §6.2D: Every failure mode mapped, observability asserted, no vendor leakage.
///
/// MANDATORY: Every adapter must certify against all required failure modes.
/// </summary>
public sealed class ${NAME}CertificationTests
{
    private readonly InMemoryTelemetryAdapter _telemetry = new();

    private static OperationContext Ctx() => new()
    {
        CorrelationId = "cert-test-001",
        OperationName = "${NAME}CertTest",
        Timestamp = DateTimeOffset.UtcNow,
        Source = OperationSource.HttpRequest
    };

    // --- Happy path ---

    [Fact]
    public async Task Should_return_normalized_result_on_success()
    {
        throw new NotImplementedException("Implement: create adapter with StubHandler returning 200 + valid JSON");
    }

    // --- Failure mode: 404 Not Found → ErrorCode.NotFound ---

    [Fact]
    public async Task Should_return_not_found_on_404()
    {
        throw new NotImplementedException("Implement: StubHandler returning 404");
    }

    // --- Failure mode: 401/403 → ErrorCode.Authorization ---

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Should_return_authorization_error_on_auth_failure(HttpStatusCode statusCode)
    {
        throw new NotImplementedException("Implement: StubHandler returning auth error");
    }

    // --- Failure mode: 429 → ErrorCode.RateLimited ---

    [Fact]
    public async Task Should_return_rate_limited_on_429()
    {
        throw new NotImplementedException("Implement: StubHandler returning 429");
    }

    // --- Failure mode: 5xx → ErrorCode.TransientFailure ---

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Should_return_transient_failure_on_5xx(HttpStatusCode statusCode)
    {
        throw new NotImplementedException("Implement: StubHandler returning 5xx");
    }

    // --- Failure mode: Timeout → ErrorCode.Timeout ---

    [Fact]
    public async Task Should_return_timeout_on_request_timeout()
    {
        throw new NotImplementedException("Implement: TimeoutHandler throwing TaskCanceledException");
    }

    // --- Failure mode: Cancellation → ErrorCode.Timeout ---

    [Fact]
    public async Task Should_return_timeout_on_cancellation()
    {
        throw new NotImplementedException("Implement: cancel CancellationTokenSource before call");
    }

    // --- Failure mode: Network error → ErrorCode.DependencyUnavailable ---

    [Fact]
    public async Task Should_return_dependency_unavailable_on_network_error()
    {
        throw new NotImplementedException("Implement: handler throwing HttpRequestException");
    }

    // --- Failure mode: Malformed payload → ErrorCode.PermanentFailure ---

    [Fact]
    public async Task Should_return_permanent_failure_on_malformed_payload()
    {
        throw new NotImplementedException("Implement: StubHandler returning 200 with invalid JSON");
    }

    // --- Observability assertions ---

    [Fact]
    public async Task Should_emit_span_with_dependency_name()
    {
        throw new NotImplementedException("Assert: _telemetry.Spans contains span with externalTarget attribute");
    }

    [Fact]
    public async Task Should_increment_external_call_counter()
    {
        throw new NotImplementedException("Assert: _telemetry.Counters contains 'external.calls' with target label");
    }

    [Fact]
    public async Task Should_log_error_with_dependency_name_on_failure()
    {
        throw new NotImplementedException("Assert: _telemetry.Logs contains error log with dependency name");
    }

    // --- No vendor leakage ---

    [Fact]
    public async Task Result_should_not_contain_vendor_specific_types()
    {
        throw new NotImplementedException("Assert: result type does not reference vendor namespace");
    }

    // --- HTTP Stub Handlers (copy and customize) ---

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public StubHandler(HttpStatusCode statusCode, string? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content is not null)
                response.Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new TaskCanceledException("Simulated timeout", new TimeoutException());
    }

    private sealed class NetworkErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("Simulated network failure");
    }
}
CSHARP

        echo "Created:"
        echo "  src/DevOpsSite.Adapters/${NAME}/${NAME}Adapter.cs"
        echo "  src/DevOpsSite.Adapters/${NAME}/${NAME}Config.cs"
        echo "  tests/DevOpsSite.Adapters.Tests/${NAME}/${NAME}AdapterTests.cs"
        echo "  tests/DevOpsSite.Adapters.Tests/${NAME}/${NAME}CertificationTests.cs"
        echo ""
        echo "Next steps:"
        echo "  1. Implement port methods in adapter"
        echo "  2. Map ALL vendor errors to error taxonomy (see certification tests)"
        echo "  3. Add telemetry spans for every external call"
        echo "  4. Configure adapter in contract test"
        echo "  5. Fill in ALL certification test stubs — every NotImplementedException must be resolved"
        echo "  6. Run: dotnet test --filter ${NAME}"
        echo ""
        echo "MANDATORY: Adapter cannot ship until all certification tests pass."
        ;;

    endpoint)
        if [ $# -lt 3 ]; then
            echo "Usage: ./scaffolds/scaffold.sh endpoint <Name> <Path>"
            exit 1
        fi
        URL_PATH="$3"
        echo "Scaffolding endpoint: $NAME at $URL_PATH"

        mkdir -p "$ROOT_DIR/src/DevOpsSite.Host/Routes"
        cat > "$ROOT_DIR/src/DevOpsSite.Host/Routes/${NAME}Routes.cs" << CSHARP
using DevOpsSite.Host.Middleware;

namespace DevOpsSite.Host.Routes;

/// <summary>
/// Transport shell for ${NAME}. Constitution §12: thin shell only.
/// Parse input -> invoke use case -> map response. No business logic.
/// </summary>
public static class ${NAME}Routes
{
    public static void Map${NAME}Routes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("${URL_PATH}", async (HttpContext httpContext) =>
        {
            var ctx = httpContext.GetOperationContext() with
            {
                OperationName = "${NAME}"
            };

            // Invoke use case handler here
            // Map Result<T> to HTTP response
            return Results.Ok();
        });
    }
}
CSHARP

        echo "Created:"
        echo "  src/DevOpsSite.Host/Routes/${NAME}Routes.cs"
        echo ""
        echo "Next steps:"
        echo "  1. Inject use case handler"
        echo "  2. Map input from request"
        echo "  3. Map Result<T> to HTTP response"
        echo "  4. Register in Program.cs: app.Map${NAME}Routes();"
        ;;

    regression)
        echo "Scaffolding regression test: $NAME"

        mkdir -p "$ROOT_DIR/tests/DevOpsSite.Domain.Tests/Regression"
        cat > "$ROOT_DIR/tests/DevOpsSite.Domain.Tests/Regression/${NAME}.RegressionTests.cs" << CSHARP
namespace DevOpsSite.Domain.Tests.Regression;

/// <summary>
/// Regression test for bug: ${NAME}
/// Constitution §6.2E: Every production bug must begin with a failing test.
///
/// Bug source: [describe the bug and how it was discovered]
/// Invariant protected: [describe what invariant was violated]
/// </summary>
public sealed class ${NAME}RegressionTests
{
    [Fact]
    public void Should_reproduce_original_failure()
    {
        // Arrange: set up the conditions that caused the bug
        // Act: execute the operation that triggered the bug
        // Assert: verify the bug manifests
        throw new NotImplementedException("Write failing test that reproduces the bug.");
    }

    [Fact]
    public void Should_pass_after_fix()
    {
        // Arrange: same conditions as above
        // Act: execute the operation
        // Assert: verify correct behavior after fix
        throw new NotImplementedException("Write test that passes after the fix.");
    }
}
CSHARP

        echo "Created:"
        echo "  tests/DevOpsSite.Domain.Tests/Regression/${NAME}.RegressionTests.cs"
        echo ""
        echo "Next steps:"
        echo "  1. Write the failing test FIRST (it must fail before the fix)"
        echo "  2. Implement the fix"
        echo "  3. Verify the test passes"
        ;;

    *)
        echo "Unknown scaffold type: $TYPE"
        echo "Valid types: capability, port, adapter, endpoint, regression"
        exit 1
        ;;
esac
