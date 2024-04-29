using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IReadOnlyList<TestInformation> AllTests { get; init; }
    public required Queue<TestInformation> NotInParallel { get; init; }
    public required List<NotInParallelTestCase> KeyedNotInParallel { get; init; }

    public required Queue<TestInformation> Parallel { get; init; }
    
    public int TestCount => AllTests.Count;
}