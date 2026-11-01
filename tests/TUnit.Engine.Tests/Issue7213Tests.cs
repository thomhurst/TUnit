using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// When tracking a test's object graph throws in a nested property getter, the test's root argument
/// objects are still disposed, for a test that fails during registration and for a skipped test (#7213).
/// </summary>
public class Issue7213Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Roots_Are_Disposed_When_Tracking_Throws()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7213/*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Passed").TestName
                    .ShouldBe("Roots_Are_Disposed_When_Tracking_Throws")
            ]);
    }
}
