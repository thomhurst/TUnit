using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Bugs;

public class Bug2136(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2136/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(5),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}