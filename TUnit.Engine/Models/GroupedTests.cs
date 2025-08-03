using TUnit.Core;
using TUnit.Engine.Scheduling;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IList<AbstractExecutableTest> Parallel { get; init; }
    
    public required PriorityQueue<AbstractExecutableTest, TestPriority> NotInParallel { get; init; }
    
    public required IDictionary<string, PriorityQueue<AbstractExecutableTest, TestPriority>> KeyedNotInParallel { get; init; }
    
    public required IDictionary<string, SortedDictionary<int, List<AbstractExecutableTest>>> ParallelGroups { get; init; }
}
