using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.Engine.Models;

internal record GroupedTests
{
    public required Queue<TestCase> NotInParallel { get; init; }
    public required Queue<IGrouping<string, TestCase>> KeyedNotInParallel { get; init; }

    public required Queue<TestCase> Parallel { get; init; }

    public required IReadOnlyList<TestCase> LastTestOfClasses { get; init; }
}