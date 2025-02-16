using Shouldly;

namespace TUnit.Engine.Tests;

public class ComplexDependsOnTests2 : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*ComplexDependsOn2/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(112),
                result => result.ResultSummary.Counters.Passed.ShouldBe(112),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}