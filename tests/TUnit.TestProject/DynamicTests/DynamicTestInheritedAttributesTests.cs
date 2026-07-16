using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

[NotInParallel]
public abstract class NotInParallelBaseClass;

[EngineTest(ExpectedResult.Pass)]
[Retry(3)]
public class DynamicTestInheritedAttributesTests : NotInParallelBaseClass
{
    private static readonly ConcurrentBag<DateTimeRange> TestDateTimeRanges = [];

    public async Task TestMethod()
    {
        await Task.Delay(300);
        TestDateTimeRanges.Add(new DateTimeRange(
            TestContext.Current!.Execution.TestStart!.Value.DateTime,
            DateTime.UtcNow));
    }

    [After(Class)]
    public static async Task VerifyNoOverlaps()
    {
        // Wait a bit to ensure all test times are recorded
        await Task.Delay(100);

        foreach (var testDateTimeRange in TestDateTimeRanges)
        {
            await Assert.That(TestDateTimeRanges
                .Except([testDateTimeRange])
                .Any(x => x.Overlap(testDateTimeRange)))
                .IsFalse()
                .Because("Dynamic tests should inherit [NotInParallel] from base class and not run in parallel");
        }
    }

#pragma warning disable TUnitWIP0001
    [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
    public static void BuildTests(DynamicTestBuilderContext context)
    {
        // Create multiple dynamic tests - they should NOT run in parallel
        // because the base class has [NotInParallel]
        for (var i = 0; i < 3; i++)
        {
            context.AddTest(new DynamicTest<DynamicTestInheritedAttributesTests>
            {
                TestMethod = @class => @class.TestMethod(),
                TestMethodArguments = [],
                DisplayName = $"DynamicTest_InheritedNotInParallel_{i}",
                Attributes = []
            });
        }
    }

    private class DateTimeRange(DateTime start, DateTime end)
    {
        public DateTime Start { get; } = start;
        public DateTime End { get; } = end;

        public bool Overlap(DateTimeRange other)
        {
            return Start < other.End && other.Start < End;
        }
    }
}
