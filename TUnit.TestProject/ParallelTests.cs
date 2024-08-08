using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class ParallelTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];

    [Test, Repeat(3)]
    public async Task Parallel_Test1()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));

        await AssertOverlaps();
    }
    
    [Test, Repeat(3)]
    public async Task Parallel_Test2()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));

        await AssertOverlaps();
    }
    
    [Test, Repeat(3)]
    public async Task Parallel_Test3()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));

        await AssertOverlaps();
    }

    private async Task AssertOverlaps()
    {
        foreach (var testDateTimeRange in TestDateTimeRanges)
        {
            await Assert.That(TestDateTimeRanges
                .Except([testDateTimeRange])
                .Any(x => x.Overlap(testDateTimeRange)))
                .Is.True();
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