using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

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
    
    public IParallelConstraint? Constraint { get; init; }
    public string? ConstraintKey { get; init; }
    public int Order { get; init; }
    public Priority Priority { get; init; }

    public TestExecutionState(ExecutableTest test)
    {
        Test = test;
        State = TestState.NotStarted;
        RemainingDependencies = test.Dependencies.Length;
        Dependents =
        [
        ];
        EnqueueTime = DateTime.UtcNow;
        
        Constraint = test.Context.ParallelConstraint;
        Order = Constraint switch
        {
            NotInParallelConstraint nip => nip.Order,
            ParallelGroupConstraint pg => pg.Order,
            _ => int.MaxValue / 2
        };
        
        ConstraintKey = Constraint switch
        {
            NotInParallelConstraint { NotInParallelConstraintKeys.Count: > 0 } nip => 
                string.Join(",", nip.NotInParallelConstraintKeys.OrderBy(k => k)),
            ParallelGroupConstraint pg => pg.Group,
            _ => null
        };
        
        Priority = test.Context.ExecutionPriority;
    }

    /// <summary>
    /// Atomically decrements the remaining dependencies count
    /// </summary>
    public int DecrementRemainingDependencies()
    {
        return Interlocked.Decrement(ref _remainingDependencies);
    }
}
