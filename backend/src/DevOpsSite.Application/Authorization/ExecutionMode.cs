namespace DevOpsSite.Application.Authorization;

/// <summary>
/// How a capability is expected to execute.
/// Determines whether the caller gets an immediate result or a job reference.
/// </summary>
public enum ExecutionMode
{
    /// <summary>Synchronous request-response. Result returned directly.</summary>
    Synchronous,

    /// <summary>Asynchronous / long-running. Returns a job reference; result retrieved later.</summary>
    Asynchronous
}
