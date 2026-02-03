using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Regression test for GitHub issue #4656: TestDiscoveryContext.AllTests incorrectly
/// includes tests when class names overlap (ABCV vs ABCVC) due to substring matching.
/// https://github.com/thomhurst/TUnit/issues/4656
/// </summary>
public class OverlappingClassNameFilterTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Filtering_ABCVC_B2_ShouldNotInclude_ABCV_B2()
    {
        // Filter for only ABCVC.B2 (and its dependency ABCVC.B0)
        // Bug #4656: ABCV.B2 was incorrectly included because "ABCV" is a substring of "ABCVC"
        // With the fix, only ABCVC tests should run (B2 + B0 dependency = 2 tests)
        // Without the fix, ABCV.B2 and ABCV.A1 (dependency) would also run (4 tests total)
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/ABCVC/B2",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result =>
                {
                    // Key assertion: exactly 2 tests should run (ABCVC.B2 + ABCVC.B0)
                    // If the bug exists, 4 tests would run (also ABCV.B2 + ABCV.A1)
                    result.ResultSummary.Counters.Total.ShouldBe(2,
                        $"Expected 2 tests (ABCVC.B2 + ABCVC.B0) but got {result.ResultSummary.Counters.Total}. " +
                        $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}. " +
                        "If more than 2 tests ran, the substring matching bug (#4656) may be present.");
                },
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filtering_ABCV_ShouldNotMatch_ABCVC()
    {
        // Filter for all tests in ABCV class (A1, B1, B2 = 3 tests)
        // Should NOT include any ABCVC tests
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/ABCV/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result =>
                {
                    // Expected: 3 tests from ABCV (A1, B1, B2)
                    // If ABCVC tests were incorrectly included, we'd have 6 tests
                    result.ResultSummary.Counters.Total.ShouldBe(3,
                        $"Expected 3 tests (ABCV.A1, ABCV.B1, ABCV.B2) but got {result.ResultSummary.Counters.Total}. " +
                        $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}");
                },
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
