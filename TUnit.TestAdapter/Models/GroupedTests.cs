namespace TUnit.TestAdapter.Models;

internal record GroupedTests
{
    public required Queue<TestWithTestCase> NotInParallel { get; init; }
    public required Queue<IGrouping<string, TestWithTestCase>> KeyedNotInParallel { get; init; }

    public required Queue<TestWithTestCase> Parallel { get; init; }

    public required IReadOnlyList<TestWithTestCase> LastTestOfClasses { get; init; }
}