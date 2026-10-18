using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7089Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Late_IsEnabled_Write_Does_Not_Change_Selected_Mode()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7089/ExecutionModeLockTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
            ]);
    }
}
