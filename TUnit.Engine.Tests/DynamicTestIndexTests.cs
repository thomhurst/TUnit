using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Tests that validate DynamicTestIndex generates unique test IDs
/// when multiple dynamic tests target the same method.
/// </summary>
public class DynamicTestIndexTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*DynamicTests/DynamicTestIndexTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(5),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
