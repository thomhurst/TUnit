using Shouldly;
using TUnit.Engine.Tests.Attributes;

namespace TUnit.Engine.Tests;

[SkipNetFramework("ExecutionContext.Restore is not supported on .NET Framework")]
public class AsyncLocalTest(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/AsyncLocalTest/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}