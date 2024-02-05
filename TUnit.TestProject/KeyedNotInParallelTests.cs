using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.TestProject;

public class KeyedNotInParallelTests
{
    private static readonly ConcurrentBag<ConstraintDateTimeRange> TestDateTimeRanges = new();

    [Test, NotInParallel("1"), Repeat(3)]
    public async Task NotInParallel_Test1()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new ConstraintDateTimeRange("1", start, end));

        await AssertNoOverlaps();
    }
    
    [Test, NotInParallel("1"), Repeat(3)]
    public async Task NotInParallel_Test2()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new ConstraintDateTimeRange("1", start, end));

        await AssertNoOverlaps();
    }
    
    [Test, NotInParallel("3"), Repeat(3)]
    public async Task NotInParallel_Test3()
    {
        var start = DateTime.Now;

        await Task.Delay(500);
        
        var end = DateTime.Now;
        
        TestDateTimeRanges.Add(new ConstraintDateTimeRange("3", start, end));

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

    private class ConstraintDateTimeRange
    {
        public string ConstraintKey { get; }
        public DateTime Start { get; }
        public DateTime End { get; }
        
        public ConstraintDateTimeRange(string constraintKey, DateTime start, DateTime end)
        {
            ConstraintKey = constraintKey;
            Start = start;
            End = end;
        }
        
        public bool Overlap(ConstraintDateTimeRange other)
        {
            return ConstraintKey == other.ConstraintKey && Start <= other.End && other.Start <= End;
        }
    }
}