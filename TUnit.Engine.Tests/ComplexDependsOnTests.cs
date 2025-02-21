using Shouldly;

namespace TUnit.Engine.Tests;

public class ComplexDependsOnTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*ComplexDependsOn/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(45),
                result => result.ResultSummary.Counters.Passed.ShouldBe(45),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}