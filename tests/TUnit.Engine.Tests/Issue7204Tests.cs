using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A test that asks for a shared instance after its last holder has disposed it gets a new
/// instance: both a dynamic test registered later and a running test that fetches it through
/// SharedDataSources (#7204).
/// </summary>
public class Issue7204Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Tests_Asking_After_Disposal_Get_A_New_Shared_Instance()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7204/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
