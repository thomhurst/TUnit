using Shouldly;

namespace TUnit.Engine.Tests.Bugs;

public class Bug1821 : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._1821/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}