using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7096Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Same_Type_Arguments_Rows_All_Run()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7096/GenericMethodSameTypeArgumentsTests/SameType",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Mixed_Type_Arguments_Rows_All_Run()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7096/GenericMethodSameTypeArgumentsTests/MixedTypes",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
