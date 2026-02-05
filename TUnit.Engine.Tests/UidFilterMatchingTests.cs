using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Comprehensive tests for UID filter matching in MetadataFilterMatcher.CouldMatchUidFilter.
/// Tests various scenarios that ensure VS Test Explorer can correctly filter tests.
/// Regression tests for GitHub issue #4656 follow-up.
/// </summary>
public class UidFilterMatchingTests(TestMode testMode) : InvokableTestBase(testMode)
{
    #region Nested Classes Tests

    [Test]
    public async Task Filter_NestedClass_ShouldMatchOnlyNestedClass()
    {
        // Filter for the nested class InnerClass
        // Tree node paths use just the innermost class name (Type.Name)
        // Should only run tests from InnerClass, not OuterClass
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/InnerClass/InnerMethod",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (InnerClass.InnerMethod) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_OuterClass_ShouldNotMatchNestedClasses()
    {
        // Filter for only the outer class method
        // Should only run OuterClass.OuterMethod, not nested class methods
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/OuterClass/OuterMethod",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (OuterClass.OuterMethod) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    #endregion

    #region Overlapping Names Tests

    [Test]
    public async Task Filter_FilterTest_ShouldNotMatchFilterTestHelper()
    {
        // Filter for FilterTest class
        // Should NOT match FilterTestHelper or FilterTesting
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/FilterTest/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (FilterTest.Method1) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}. " +
                    "If more tests ran, substring matching may be incorrectly matching FilterTestHelper or FilterTesting."),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_FilterTestHelper_ShouldNotMatchFilterTest()
    {
        // Filter for FilterTestHelper class
        // Should NOT match FilterTest
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/FilterTestHelper/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (FilterTestHelper.Method1) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    #endregion

    #region Method Name Boundary Tests

    [Test]
    public async Task Filter_MethodNameTest_ShouldNotMatchTestMethod()
    {
        // Filter for method named "Test"
        // Should NOT match "TestMethod", "MyTest", or "TestingMethod"
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/MethodNameBoundaryTests/Test",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1,
                    $"Expected 1 test (MethodNameBoundaryTests.Test) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}. " +
                    "If more tests ran, method name boundary matching may be incorrect."),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_AllMethodsInClass_ShouldMatchAllFour()
    {
        // Filter for all methods in MethodNameBoundaryTests
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/MethodNameBoundaryTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4,
                    $"Expected 4 tests but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    #endregion

    #region Original Issue Regression Tests

    [Test]
    public async Task OriginalIssue_ABCVC_B2_ShouldNotInclude_ABCV_B2()
    {
        // Original regression test from issue #4656
        // Filter for ABCVC.B2 should NOT include ABCV.B2
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._4656/ABCVC/B2",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2,
                    $"Expected 2 tests (ABCVC.B2 + ABCVC.B0 dependency) but got {result.ResultSummary.Counters.Total}. " +
                    $"Test names: {string.Join(", ", result.Results.Select(r => r.TestName))}"),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    #endregion
}
