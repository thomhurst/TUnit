using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    // Use arrays for blazingly fast iteration and zero allocation enumeration
    public required AbstractExecutableTest[] Parallel { get; init; }
    
    // Pre-sorted array by priority for ultra-fast iteration
    // Tests are already sorted, no need to store priority
    public required AbstractExecutableTest[] NotInParallel { get; init; }
    
    // Array of key-value pairs since we only iterate, never lookup by key
    // Tests within each key are pre-sorted by priority
    public required (string Key, AbstractExecutableTest[] Tests)[] KeyedNotInParallel { get; init; }
    
    // Array of groups with nested arrays for maximum iteration performance
    // Tests are grouped by order, ready for parallel execution
    public required (string Group, (int Order, AbstractExecutableTest[] Tests)[] OrderedTests)[] ParallelGroups { get; init; }
}
