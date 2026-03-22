using DevOpsSite.Application.Context;

namespace DevOpsSite.Application.Authorization;

/// <summary>
/// Central authorization evaluator. Evaluates actor context against capability requirements.
/// Constitution §14: Authorization is enforced through a standard path, not scattered checks.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Evaluate whether the given actor context is authorized for the named capability.
    /// Returns a stable AuthorizationResult — never throws for authorization failures.
    /// </summary>
    AuthorizationResult Evaluate(string operationName, OperationContext ctx);
}
