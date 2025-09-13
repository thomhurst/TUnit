namespace TUnit.Core.Models;

/// <summary>
/// Represents the execution context for tests, indicating how they should be coordinated
/// </summary>
public enum ExecutionContextType
{
    /// <summary>
    /// Tests can run in full parallel without coordination
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Tests must run sequentially, one at a time globally
    /// </summary>
    NotInParallel,
    
    /// <summary>
    /// Tests must run sequentially within the same key
    /// </summary>
    KeyedNotInParallel,
    
    /// <summary>
    /// Tests run in parallel groups with internal ordering
    /// </summary>
    ParallelGroup
}

/// <summary>
/// Execution context information for a test, used to coordinate class hooks properly
/// </summary>
public record TestExecutionContext
{
    public required ExecutionContextType ContextType { get; init; }
    
    /// <summary>
    /// For KeyedNotInParallel: the constraint key
    /// For ParallelGroup: the group name
    /// Null for other types
    /// </summary>
    public string? GroupKey { get; init; }
    
    /// <summary>
    /// For ParallelGroup: the execution order within the group
    /// Null for other types
    /// </summary>
    public int? Order { get; init; }
}