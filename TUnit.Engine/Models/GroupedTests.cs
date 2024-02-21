using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required IReadOnlyList<TestCase> AllTests { get; init; }
    public required Queue<TestCase> NotInParallel { get; init; }
    public required List<TestCase> KeyedNotInParallel { get; init; }

    public required Queue<TestCase> Parallel { get; init; }
    
    public int TestCount => AllTests.Count;
}