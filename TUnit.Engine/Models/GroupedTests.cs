using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required DiscoveredTest[] AllValidTests { get; init; }
    public required Queue<DiscoveredTest> NotInParallel { get; init; }
    public required IList<NotInParallelTestCase> KeyedNotInParallel { get; init; }
    public required Queue<DiscoveredTest> Parallel { get; init; }
    public required IDictionary<string, List<DiscoveredTest>> ParallelGroups { get; set; }
    
    public int TestCount => AllValidTests.Length;
}