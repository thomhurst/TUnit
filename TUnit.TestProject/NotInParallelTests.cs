using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class NotInParallelTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];

    [After(Test)]
    public async Task TestOverlaps()
    {
        TestDateTimeRanges.Add(new DateTimeRange(TestContext.Current!.Execution.TestStart!.Value, TestContext.Current.Execution.Result!.End!.Value));

        await AssertNoOverlaps();
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test1(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test2(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    }

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test3(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    }

    private async Task AssertNoOverlaps()
    {
        foreach (var testDateTimeRange in TestDateTimeRanges)
        {
            await Assert.That(TestDateTimeRanges
                .Except([testDateTimeRange])
                .Any(x => x.Overlap(testDateTimeRange)))
                .IsFalse();
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
