using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IList<AbstractExecutableTest> Parallel { get; init; }
    
    public required PriorityQueue<AbstractExecutableTest, int> NotInParallel { get; init; }
    
    public required IDictionary<string, PriorityQueue<AbstractExecutableTest, int>> KeyedNotInParallel { get; init; }
    
    public required IDictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>> ParallelGroups { get; init; }
}
