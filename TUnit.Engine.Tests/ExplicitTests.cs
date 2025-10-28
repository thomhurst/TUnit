using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ExplicitTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task ExplicitMethodTest_WithWildcardFilter_ShouldExcludeExplicitTests()
    {
        // When filtering with a wildcard that matches both explicit and non-explicit tests,
        // the explicit tests should be excluded
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/Test1/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ExplicitMethodTest_WithSpecificFilter_ShouldIncludeExplicitTest()
    {
        // When filtering specifically for an explicit test, it should run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/Test1/TestMethod2",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"), // Test is designed to fail
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ExplicitClassTest_WithClassFilter_ShouldIncludeAllTestsInExplicitClass()
    {
        // When filtering for an explicit class, all tests in that class should run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/ExplicitClass/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }


    [Test]
    public async Task MixedClassTest_WithClassWildcard_ShouldExcludeOnlyExplicitMethods()
    {
        // When filtering a class with mixed explicit/non-explicit tests,
        // only non-explicit tests should run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/MixedTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2), // NormalTest and SkippedTest
                result => result.ResultSummary.Counters.Passed.ShouldBe(1), // NormalTest
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1) // SkippedTest
            ]);
    }

    [Test]
    public async Task NegativeCategoryFilter_WithExplicitTestPresent_ShouldExcludePerformanceCategory()
    {
        // This test replicates GitHub issue #3190
        // Bug: When ANY test has [Explicit], negative category filters stop working correctly
        // Expected: [Category!=Performance] should exclude all tests with Performance category
        // Actual: It runs all non-explicit tests INCLUDING those with Performance category
        //
        // CORRECT BEHAVIOR (Two-Stage Filtering):
        //
        // Stage 1: Pre-filter for [Explicit]
        //   The wildcard filter "/*/*/*/*[Category!=Performance]" does NOT positively select explicit tests.
        //   Initial candidate list should only contain non-explicit tests:
        //     - TUnit.TestProject.Bugs._3190.TestClass1.TestMethod1 (has Performance)
        //     - TUnit.TestProject.Bugs._3190.TestClass1.TestMethod2 (no Performance)
        //     - TUnit.TestProject.Bugs._3190.TestClass2.TestMethod1 (has Performance)
        //     - TUnit.TestProject.Bugs._3190.TestClass3.RegularTestWithoutCategory (no Performance)
        //   NOT included:
        //     - TUnit.TestProject.Bugs._3190.TestClass2.TestMethod2 (Explicit - not positively selected)
        //
        // Stage 2: Apply negative category filter
        //   From the candidate list, exclude tests with [Category("Performance")]:
        //     ✓ TestClass1.TestMethod2 (no Performance category)
        //     ✗ TestClass1.TestMethod1 (has Performance - excluded)
        //     ✗ TestClass2.TestMethod1 (has Performance - excluded)
        //     ✓ TestClass3.RegularTestWithoutCategory (no Performance category)
        //
        // Expected result: Exactly 2 tests should run
        //   1. TUnit.TestProject.Bugs._3190.TestClass1.TestMethod2
        //   2. TUnit.TestProject.Bugs._3190.TestClass3.RegularTestWithoutCategory
        //
        // This test will FAIL until the two-stage filtering is properly implemented.
        await RunTestsWithFilter(
            "/*/*/*/*[Category!=Performance]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

}