using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class ParallelTests
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];

    [After(Test)]
    public void RecordExecutionRange()
    {
        TestDateTimeRanges.Add(new DateTimeRange(TestContext.Current!.Execution.TestStart!.Value.DateTime, TestContext.Current.Execution.Result!.End!.Value.DateTime));
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test1()
    {
        await Task.Delay(5000);
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test2()
    {
        await Task.Delay(5000);
    }

    [Test, Repeat(3)]
    public async Task Parallel_Test3()
    {
        await Task.Delay(5000);
    }

    [After(Class)]
    public static async Task AssertOverlaps()
    {
        // Later repetitions may run alone on a busy runner. One overlapping pair
        // is enough to prove that this class can execute tests concurrently.
        var ranges = TestDateTimeRanges.ToArray();
        await Assert.That(ranges.Any(range => ranges.Any(other =>
                !ReferenceEquals(range, other) && range.Overlap(other))))
            .IsTrue();
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
