using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Objects that the builder created for a skipped test or a skipped data row are disposed. A keyed
/// instance that another test still uses stays alive (#7156).
/// </summary>
public class Issue7156Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Objects_Of_Skipped_Rows_Are_Disposed_But_Shared_Ones_Stay_Alive()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7156/*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(3),
                result => result.Results.Single(x => x.Outcome == "Passed").TestName
                    .ShouldBe("Objects_Of_Skipped_Rows_Are_Disposed_But_Shared_Ones_Stay_Alive")
            ]);
    }
}
