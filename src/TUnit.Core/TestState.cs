namespace TUnit.Core;

/// <summary>
/// Represents the possible states of a test during its lifecycle.
/// </summary>
public enum TestState
{
    /// <summary>
    /// The test has not yet started execution.
    /// </summary>
    NotStarted,

    /// <summary>
    /// The test is waiting for its dependencies to complete before it can execute.
    /// </summary>
    WaitingForDependencies,

    /// <summary>
    /// The test is queued and waiting for an available execution slot.
    /// </summary>
    Queued,

    /// <summary>
    /// The test is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// The test completed successfully.
    /// </summary>
    Passed,

    /// <summary>
    /// The test failed due to an assertion failure or unhandled exception.
    /// </summary>
    Failed,

    /// <summary>
    /// The test was skipped and did not execute.
    /// </summary>
    Skipped,

    /// <summary>
    /// The test exceeded its configured timeout duration.
    /// </summary>
    Timeout,

    /// <summary>
    /// The test was cancelled before completion.
    /// </summary>
    Cancelled
}
