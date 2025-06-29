using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class CustomFilteringTests4(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/CustomFilteringTests/*[one=other]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(0),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
