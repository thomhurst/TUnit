using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue6885Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Next_Test_Starts_Only_After_DedicatedThreadExecutor_CleanUp_Returns()
    {
        await RunTestsWithFilter(
            "/*/*/DedicatedThreadExecutorCleanUpOrderTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
