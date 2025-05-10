using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DependsOnAndNotInParallelTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnAndNotInParallelTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),

            ]);
    }
}