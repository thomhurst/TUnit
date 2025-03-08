using Shouldly;

namespace TUnit.Engine.Tests;

public class MatrixTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        var expectedCount = IsNetFramework ? 122 : 260;
        
        await RunTestsWithFilter(
            "/*/*/MatrixTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedCount, "Total"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedCount, "Passed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0, "Failed"),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0, "Skipped")
            ]);
    }
}