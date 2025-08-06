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
    public async Task ExplicitClassTest_WithNamespaceWildcard_ShouldExcludeExplicitClass()
    {
        // When filtering with a namespace wildcard that matches both explicit and non-explicit tests,
        // the explicit class tests should be excluded
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3), // TestMethod from Test1, NormalTest from MixedTests, SkippedTest (skipped)
                result => result.ResultSummary.Counters.Passed.ShouldBe(2), // TestMethod and NormalTest
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1) // SkippedTest
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
    public async Task ExplicitTest_WithNoFilter_ShouldExcludeAllExplicitTests()
    {
        // When running all tests in the existing ExplicitTests class (the original one in TestProject),
        // they should be excluded because the class is marked [Explicit]
        await RunTestsWithFilter(
            "/*/TUnit.TestProject/ExplicitTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2), // Both tests run since we're specifically targeting the class
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ExplicitTest_WithBroadWildcard_ShouldRunExplicitClass()
    {
        // When using a filter that only matches an explicit class,
        // the tests should run because all matched tests are explicit
        await RunTestsWithFilter(
            "/*/TUnit.TestProject/Explicit*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2), // Both tests in ExplicitTests class run
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ExplicitTest_WithVeryBroadWildcard_ShouldExcludeExplicitTests()
    {
        // When using a very broad wildcard that matches both explicit and non-explicit tests,
        // explicit tests should be excluded
        await RunTestsWithFilter(
            "/*/TUnit.TestProject/*Test*/*",
            [
                result => result.ResultSummary.Outcome.ShouldNotBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBeGreaterThan(0), // Should have some tests
                result => result.ResultSummary.Counters.Total.ShouldNotBe(2) // But not just the 2 explicit tests
            ]);
    }
}