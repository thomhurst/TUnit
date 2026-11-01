using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A tuple row binds whole to a tuple parameter followed by optional or params parameters (#7212).
/// </summary>
public class Issue7212Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Tuple_Parameter_Followed_By_Optional_Parameters_Binds_Whole()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7212/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(8),
                result => result.ResultSummary.Counters.Passed.ShouldBe(8),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
