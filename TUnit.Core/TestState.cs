namespace TUnit.Core;

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
