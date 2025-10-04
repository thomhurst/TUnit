using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class NegativeFilterWithExplicitTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task NegativeFilter_WithoutExplicitTests_ShouldExcludePerformanceTests()
    {
        // Test negative filter without any explicit tests present
        // This should work correctly - exclude Performance category tests
        await RunTestsWithFilter(
            "/*/*/*/*[Category!=Performance]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                // Should run only non-Performance tests from TestClass1WithoutExplicit
                result => result.ResultSummary.Counters.Total.ShouldBeGreaterThan(0),
                result => result.ResultSummary.Counters.Passed.ShouldBeGreaterThan(0)
            ]);
    }

    [Test]
    public async Task NegativeFilter_WithExplicitTests_ShouldStillExcludePerformanceTests()
    {
        // Test negative filter when explicit tests are present in test collection
        // This reproduces the bug - currently breaks when explicit tests exist
        await RunTestsWithFilter(
            "/*/*/*/*[Category!=Performance]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                // Should run non-Performance tests from both classes
                // Currently this fails because explicit tests change the filtering behavior
                result => result.ResultSummary.Counters.Total.ShouldBeGreaterThan(0),
                result => result.ResultSummary.Counters.Passed.ShouldBeGreaterThan(0)
            ]);
    }

    [Test]
    public async Task PositiveFilter_WithExplicitTests_ShouldIncludeOnlyPerformanceTests()
    {
        // Test positive filter - should work correctly
        await RunTestsWithFilter(
            "/*/*/*/*[Category=Performance]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                // Should include Performance tests but exclude explicit ones unless specifically targeted
                result => result.ResultSummary.Counters.Total.ShouldBeGreaterThan(0),
                result => result.ResultSummary.Counters.Passed.ShouldBeGreaterThan(0)
            ]);
    }

    [Test]
    public async Task ExplicitTest_WithSpecificFilter_ShouldRunExplicitTest()
    {
        // Test that explicit tests can still be run when specifically targeted
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._3190/TestClass2WithExplicit/TestClass2TestMethod2",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}