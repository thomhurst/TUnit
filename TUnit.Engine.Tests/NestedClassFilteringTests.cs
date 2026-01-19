using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Tests for nested class filtering support.
/// This validates that tests in nested classes can be filtered using the OuterClass+NestedClass syntax.
/// </summary>
public class NestedClassFilteringTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task FilterByNestedClassName_ShouldFindNestedTest()
    {
        // Filter using the nested class name with '+' separator
        // This is what Visual Studio sends when running a test in a nested class
        await RunTestsWithFilter(
            "/*/*/NestedTestClassTests+NestedClass/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task FilterByNestedClassMethodName_ShouldFindNestedTest()
    {
        // Filter for the specific test method in the nested class
        await RunTestsWithFilter(
            "/*/*/NestedTestClassTests+NestedClass/Inner",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task FilterByOuterClassName_ShouldFindOuterTest()
    {
        // Filter for the outer class test (not nested)
        await RunTestsWithFilter(
            "/*/*/NestedTestClassTests/Outer",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
