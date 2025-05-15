using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class MatrixTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        var expectedCount = IsNetFramework ? 133 : 271;
        
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