using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Tracks the execution state of a test, delegating to the test context for shared state
/// </summary>
public sealed class TestExecutionState
{
    public AbstractExecutableTest Test { get; }
    
    // Delegate State to the actual test to ensure consistency
    public TestState State 
    { 
        get => Test.State;
        set => Test.State = value;
    }
    
    // These are scheduling-specific fields not in the test context
    public HashSet<string> Dependents { get; }
    public DateTime EnqueueTime { get; set; }
    public CancellationTokenSource? TimeoutCts { get; set; }
    
    // Delegate to test context for these properties
    public IParallelConstraint? Constraint => Test.Context.ParallelConstraint;
    public Priority Priority => Test.Context.ExecutionPriority;
    
    // These are derived from the constraint and cached for performance
    public string? ConstraintKey { get; init; }
    public int Order { get; init; }

    public TestExecutionState(AbstractExecutableTest test)
    {
        Test = test;
        Dependents = new HashSet<string>();
        EnqueueTime = DateTime.UtcNow;
        
        // Cache derived values
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
    }

}
