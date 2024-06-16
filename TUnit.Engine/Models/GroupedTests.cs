using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required List<DiscoveredTest> AllTests { get; init; }
    public required Queue<DiscoveredTest> NotInParallel { get; init; }
    public required List<NotInParallelTestCase> KeyedNotInParallel { get; init; }

    public required Queue<DiscoveredTest> Parallel { get; init; }
    
    public int TestCount => AllTests.Count;
}