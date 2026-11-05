using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A tuple row binds whole to a tuple parameter followed by optional or params parameters, from object[] and typed sources (#7212, #7340).
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
                result => result.ResultSummary.Counters.Total.ShouldBe(55),
                result => result.ResultSummary.Counters.Passed.ShouldBe(55),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
    [Test]
    public async Task Retained_Nested_Tuple_Runtime_Inference()
    {
        if (testMode != TestMode.Reflection)
        {
            return;
        }

        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7212Runtime/*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
