using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class SkipTestExceptionTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task ThrowingSkipTestException_ShouldMarkAsSkipped()
    {
        await RunTestsWithFilter(
            "/*/*/SkipExceptionFixTest/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Passed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }
}