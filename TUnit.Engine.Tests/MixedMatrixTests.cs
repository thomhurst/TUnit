using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class MixedMatrixTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/MixedMatrixTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(864, "Total"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(864, "Passed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0, "Failed"),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0, "Skipped")
            ]);
    }
}