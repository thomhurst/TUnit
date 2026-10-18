using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// Regression for https://github.com/thomhurst/TUnit/issues/7088.
/// Keyed [NotInParallel] tests must start in [ExecutionPriority] order, then ascending Order.
/// Each test in the test project fails if it starts out of order.
/// </summary>
public class KeyedNotInParallelOrderTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("KeyedNotInParallelOrderTests")]
    [Arguments("GroupedKeyedNotInParallelOrderTests")]
    public async Task KeyedNotInParallel_RunsInPriorityThenOrder(string className)
    {
        await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7088/{className}/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(6),
            result => result.ResultSummary.Counters.Passed.ShouldBe(6),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ]);
    }
}
