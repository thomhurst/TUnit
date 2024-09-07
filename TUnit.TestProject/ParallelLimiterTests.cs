using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

[ParallelLimiter<ParallelLimit3>]
public class ParallelLimiterTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];
    
    [After(Test)]
    public async Task After()
    {
        TestDateTimeRanges.Add(new DateTimeRange(TestContext.Current!.TestStart!.Value.DateTime, TestContext.Current.Result!.End.DateTime));

        await AssertOverlaps();
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test1()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));
    }
    
    [Test, Repeat(3)]
    public async Task Parallel_Test2()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));
    }
    
    [Test, Repeat(3)]
    public async Task Parallel_Test3()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new DateTimeRange(start, end));
    }
    
    [After(Class)]
    public static async Task AssertOverlaps()
    {
        var start = TestDateTimeRanges.MinBy(x => x.Start)!.Start;
        var end = TestDateTimeRanges.MaxBy(x => x.End)!.End;

        await Assert.That(end - start).Is.GreaterThan(TimeSpan.FromMilliseconds(1500));
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
    }
}