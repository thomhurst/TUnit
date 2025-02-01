using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IReadOnlyCollection<DiscoveredTest> AllValidTests { get; init; }
    public required PriorityQueue<DiscoveredTest, int> NotInParallel { get; init; }
    public required IDictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>> KeyedNotInParallel { get; init; }
    public required IList<DiscoveredTest> Parallel { get; init; }
    public required IDictionary<string, List<DiscoveredTest>> ParallelGroups { get; set; }
}