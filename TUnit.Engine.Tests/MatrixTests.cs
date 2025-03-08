using Shouldly;

namespace TUnit.Engine.Tests;

public class MatrixTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/MatrixTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(264, "Total"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(264, "Passed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0, "Failed"),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0, "Skipped")
            ]);
    }
}