using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class ParallelTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];

    [After(Test)]
    public async Task TestOverlaps()
    {
        TestDateTimeRanges.Add(new DateTimeRange(TestContext.Current!.Execution.TestStart!.Value, TestContext.Current.Execution.Result!.End!.Value));

        await AssertOverlaps();
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test1(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5000), cancellationToken);
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test2(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5000), cancellationToken);
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test3(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(5000), cancellationToken);
    }

    private async Task AssertOverlaps()
    {
        if (TestDateTimeRanges.Count < 5)
        {
            return;
        }

        foreach (var testDateTimeRange in TestDateTimeRanges)
        {
            await Assert.That(TestDateTimeRanges
                .Except([testDateTimeRange])
                .Any(x => x.Overlap(testDateTimeRange)))
                .IsTrue();
        }
    }

    private class DateTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        public DateTimeOffset Start { get; } = start;
        public DateTimeOffset End { get; } = end;

        public bool Overlap(DateTimeRange other)
        {
            return Start <= other.End && other.Start <= End;
        }
    }
}
