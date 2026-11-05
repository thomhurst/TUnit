using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7388Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Null_Method_Data_Sources_Run_Once_Or_Skip()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7388/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(33),
                result => result.ResultSummary.Counters.Passed.ShouldBe(17),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(16)
            ]);
    }
}
