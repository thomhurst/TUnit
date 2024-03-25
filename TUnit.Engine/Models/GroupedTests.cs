using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IReadOnlyList<TestNode> AllTests { get; init; }
    public required Queue<TestNode> NotInParallel { get; init; }
    public required List<NotInParallelTestCase> KeyedNotInParallel { get; init; }

    public required Queue<TestNode> Parallel { get; init; }
    
    public int TestCount => AllTests.Count;
}