using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Verifies that tests are correctly skipped when transitive dependencies fail.
/// Tests the fix for GitHub issue #4643.
/// </summary>
public class TransitiveDependenciesTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/TransitiveDependenciesTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1), // Only Test1 should fail
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(2) // Test2 and Test3 should be skipped
            ]);
    }
}
