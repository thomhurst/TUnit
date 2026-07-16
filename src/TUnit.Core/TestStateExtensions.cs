namespace TUnit.Core;

public static class TestStateExtensions
{
    public static bool IsFinal(this TestState state) =>
        state is TestState.Passed 
            or TestState.Failed 
            or TestState.Skipped 
            or TestState.Timeout 
            or TestState.Cancelled;

    public static bool IsTransient(this TestState state) =>
        state is TestState.NotStarted 
            or TestState.WaitingForDependencies 
            or TestState.Queued 
            or TestState.Running;

    public static bool IsSuccess(this TestState state) =>
        state is TestState.Passed or TestState.Skipped;

    public static bool IsFailure(this TestState state) =>
        state is TestState.Failed or TestState.Timeout;
}