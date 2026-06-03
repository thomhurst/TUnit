using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// End-to-end regression tests for GitHub discussion #6151.
/// A PerTestSession shared fixture must be disposed when only a subset of the tests that
/// consume it executes. The [Explicit] sibling TestB is built (incrementing the fixture's
/// ref count at build time) but excluded from execution — the same built-but-not-run shape
/// an IDE's uid filter produces when running a single [Arguments] case. Previously the
/// never-executed test's ref count kept the fixture alive forever, so DisposeAsync never ran.
/// </summary>
public class FilteredSharedFixtureDisposalTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Running_Subset_Of_Fixture_Consumers_Disposes_PerTestSession_Fixture()
    {
        // "/*" matches both tests, so both are built; TestB is then dropped post-build
        // because it is [Explicit] and a non-explicit test also matched.
        var markerPath = Path.Combine(Path.GetTempPath(), $"tunit-bug-6151-{Guid.NewGuid():N}.txt");

        try
        {
            await RunTestsWithFilter(
                "/*/*/Bug6151FilteredDisposalTests/*",
                [
                    result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                    result => result.ResultSummary.Counters.Total.ShouldBe(1,
                        $"Expected only TestA to run (TestB is [Explicit]). Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                    _ => File.Exists(markerPath).ShouldBeTrue($"Marker file '{markerPath}' was not written by the After(TestSession) hook"),
                    _ => File.ReadAllText(markerPath).ShouldBe("Created=1;Disposed=1")
                ],
                new RunOptions().WithEnvironmentVariable("TUNIT_BUG_6151_MARKER_PATH", markerPath));
        }
        finally
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }
    }

    [Test]
    public async Task Running_Single_Fixture_Consumer_Directly_Disposes_PerTestSession_Fixture()
    {
        // Sanity check — a literal method filter pre-filters at the metadata level, so only
        // TestA is ever built. This path was already green; guard against regression.
        var markerPath = Path.Combine(Path.GetTempPath(), $"tunit-bug-6151-{Guid.NewGuid():N}.txt");

        try
        {
            await RunTestsWithFilter(
                "/*/*/Bug6151FilteredDisposalTests/TestA",
                [
                    result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                    result => result.ResultSummary.Counters.Total.ShouldBe(1),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                    _ => File.Exists(markerPath).ShouldBeTrue($"Marker file '{markerPath}' was not written by the After(TestSession) hook"),
                    _ => File.ReadAllText(markerPath).ShouldBe("Created=1;Disposed=1")
                ],
                new RunOptions().WithEnvironmentVariable("TUNIT_BUG_6151_MARKER_PATH", markerPath));
        }
        finally
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }
        }
    }
}
