using Shouldly;

namespace TUnit.Engine.Tests;

public class FailTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/PassFailTests/*[Category=Fail]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(86),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(86),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}