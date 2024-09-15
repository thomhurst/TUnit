using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class KeyedNotInParallelTests
{
    private static readonly ConcurrentBag<ConstraintDateTimeRange> TestDateTimeRanges = [];

    [After(Test)]
    public async Task TestOverlaps()
    {
        TestDateTimeRanges.Add(new ConstraintDateTimeRange(TestContext.Current!.TestDetails.TestName, TestContext.Current!.TestStart!.Value.DateTime, TestContext.Current.Result!.End.DateTime));

        await AssertNoOverlaps();
    }
    
    [Test, NotInParallel("1"), Repeat(3)]
    public async Task NotInParallel_Test1()
    {
        await Task.Delay(500);
    }
    
    [Test, NotInParallel("1"), Repeat(3)]
    public async Task NotInParallel_Test2()
    {
        await Task.Delay(500);
    }
    
    [Test, NotInParallel("3"), Repeat(3)]
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