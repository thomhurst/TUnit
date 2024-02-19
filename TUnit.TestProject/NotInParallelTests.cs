using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class NotInParallelTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = new();

    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test1()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));

        await AssertNoOverlaps();
    }
    
    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test2()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));

        await AssertNoOverlaps();
    }
    
    [Test, NotInParallel, Repeat(3)]
    public async Task NotInParallel_Test3()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));

        await AssertNoOverlaps();
    }

    private async Task AssertNoOverlaps()
    {
        foreach (var testDateTimeRange in TestDateTimeRanges)
        {
            await Assert.That(TestDateTimeRanges
                .Except([testDateTimeRange])
                .Any(x => x.Overlap(testDateTimeRange)))
                .Is.False();
        }
    }

    private class DateTimeRange
    {
        public DateTime Start { get; }
        public DateTime End { get; }
        
        public DateTimeRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
        
        public bool Overlap(DateTimeRange other)
        {
            return Start <= other.End && other.Start <= End;
        }
    }
}