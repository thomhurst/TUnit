using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    // Use arrays for blazingly fast iteration and zero allocation enumeration
    public required AbstractExecutableTest[] Parallel { get; init; }
    
    // Pre-sorted array by priority for ultra-fast iteration
    // Tests are already sorted, no need to store priority
    public required AbstractExecutableTest[] NotInParallel { get; init; }
    
    // Array of tests with their constraint keys - no duplication
    // Tests are pre-sorted by priority
    public required (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] KeyedNotInParallel { get; init; }
    
    // Array of groups with nested arrays for maximum iteration performance
    // Tests are grouped by order, ready for parallel execution
    public required Dictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>> ParallelGroups { get; init; }
    
    // New: For tests that have both ParallelGroup and keyed NotInParallel constraints
    // Key is the parallel group name, value contains tests partitioned by constraints
    public required Dictionary<string, GroupedConstrainedTests> ConstrainedParallelGroups { get; init; }
}

/// <summary>
/// Represents tests within a parallel group that have additional constraints
/// </summary>
internal record GroupedConstrainedTests
{
    /// <summary>
    /// Tests that only have ParallelGroup constraint (can run in parallel within the group)
    /// </summary>
    public required AbstractExecutableTest[] UnconstrainedTests { get; init; }
    
    /// <summary>
    /// Tests that have both ParallelGroup and NotInParallel constraints
    /// These must respect their constraint keys even within the parallel group
    /// </summary>
    public required (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] KeyedTests { get; init; }
}
