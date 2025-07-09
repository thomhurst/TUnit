using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IList<ExecutableTest> Parallel { get; init; }
    
    public required PriorityQueue<ExecutableTest, int> NotInParallel { get; init; }
    
    public required IDictionary<string, PriorityQueue<ExecutableTest, int>> KeyedNotInParallel { get; init; }
    
    public required IDictionary<string, SortedDictionary<int, List<ExecutableTest>>> ParallelGroups { get; init; }
}
