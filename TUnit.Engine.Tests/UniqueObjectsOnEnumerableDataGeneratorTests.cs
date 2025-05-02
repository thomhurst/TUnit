using Shouldly;

namespace TUnit.Engine.Tests;

public class UniqueObjectsOnEnumerableDataGeneratorTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/UniqueObjectsOnEnumerableDataGeneratorTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(9),
                result => result.ResultSummary.Counters.Passed.ShouldBe(9),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}