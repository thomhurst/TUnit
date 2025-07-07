namespace TUnit.Core;

/// <summary>
/// Test execution state
/// </summary>
public enum TestState
{
    NotStarted,
    WaitingForDependencies,
    Queued,
    Running,
    Passed,
    Failed,
    Skipped,
    Timeout,
    Cancelled
}
