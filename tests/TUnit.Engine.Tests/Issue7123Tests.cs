using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Objects that a data-source row created are disposed when the row ends as a failed test, here
/// because its inferred type argument breaks a generic constraint. A keyed instance that another
/// test still uses stays alive (#7123).
/// </summary>
public class Issue7123Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Objects_Of_Failed_Rows_Are_Disposed_But_Shared_Ones_Stay_Alive()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7123/*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(3),
                result => result.Results.Where(x => x.Outcome == "Failed").ShouldAllBe(x => x.TestName == "Run"),
                result => result.Results.Single(x => x.Outcome == "Passed").TestName
                    .ShouldBe("Objects_Of_Failed_Rows_Are_Disposed_But_Shared_Ones_Stay_Alive")
            ]);
    }
}
