using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class KeyedDataSourceTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task KeyedDataSourceTests_ShouldPass()
    {
        await RunTestsWithFilter(
            "/*/*/KeyedDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
