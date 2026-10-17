using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7076Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Arguments_Skip_Only_Skips_Its_Own_Row()
    {
        await RunTestsWithFilter(
            "/*/*/ArgumentsRowSkipTests/OnlyMarkedRowIsSkipped",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Arguments_DisplayName_Only_Applies_To_Its_Own_Row()
    {
        await RunTestsWithFilter(
            "/*/*/ArgumentsRowSkipTests/DisplayNameOnlyAppliesToItsOwnRow",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
