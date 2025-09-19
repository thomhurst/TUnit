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
}
