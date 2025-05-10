using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class MatrixTests1(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/MatrixTests/MatrixTest_One",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(24, "Total"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(24, "Passed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0, "Failed"),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0, "Skipped")
            ]);
    }
}