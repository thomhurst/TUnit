namespace TUnit.Engine.Scheduling;

/// <summary>
/// Tracks the execution state of a test
/// </summary>
public sealed class TestExecutionState
{
    public ExecutableTest Test { get; }
    public TestState State { get; set; }
    private int _remainingDependencies;
    public int RemainingDependencies
    {
        get => _remainingDependencies;
        set => _remainingDependencies = value;
    }
    public HashSet<string> Dependents { get; }
    public DateTime EnqueueTime { get; set; }
    public CancellationTokenSource? TimeoutCts { get; set; }

    public TestExecutionState(ExecutableTest test)
    {
        Test = test;
        State = TestState.NotStarted;
        RemainingDependencies = test.Dependencies.Length;
        Dependents =
        [
        ];
        EnqueueTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Atomically decrements the remaining dependencies count
    /// </summary>
    public int DecrementRemainingDependencies()
    {
        return Interlocked.Decrement(ref _remainingDependencies);
    }
}
