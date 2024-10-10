using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class NotInParallelTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];

    [After(Test)]
    public async Task TestOverlaps()
    {
        TestDateTimeRanges.Add(new DateTimeRange(TestContext.Current!.TestStart!.Value.DateTime, TestContext.Current.Result!.End.DateTime));

        await AssertNoOverlaps();
    }
    
    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test1()
    {
        await Task.Delay(500);
    }
    
    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test2()
    {
        await Task.Delay(500);
    }
    
    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test3()
    {
        await Task.Delay(500);
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

    private class DateTimeRange(DateTime start, DateTime end)
    {
        public DateTime Start { get; } = start;
        public DateTime End { get; } = end;

        public bool Overlap(DateTimeRange other)
        {
            return Start <= other.End && other.Start <= End;
        }
    }
}