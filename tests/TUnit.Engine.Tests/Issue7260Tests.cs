using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A finished test's class instance is collectable before the session ends (#7260).
/// </summary>
public class Issue7260Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Finished_Test_Instance_Is_Collectable_Before_Session_Ends()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7260/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
