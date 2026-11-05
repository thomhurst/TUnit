using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7437Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("InvalidClass", "Run")]
    [Arguments("InvalidMethod", "GenericRun")]
    [Arguments("UninferredClass", "Run")]
    public async Task Discovery_Failure_Preserves_Filter_Attributes(string className, string methodName)
    {
        var path = $"/*/TUnit.TestProject.Bugs._7437/{className}*/{methodName}";
        // Reflection can infer this source's object row at runtime; generated discovery reports
        // the compile-time inference failure. Both must retain the selected filter properties.
        var expectedFailed = className == "UninferredClass" && testMode == TestMode.Reflection ? 0 : 1;
        await AssertSelected(path, expectedFailed);
        foreach (var scope in new[] { "Assembly", "Base", "Class", "Method" })
        {
            await AssertSelected($"{path}[Category=Issue7437{scope}]", expectedFailed);
            await AssertSelected($"{path}[Issue7437Scope={scope}]", expectedFailed);
            await RunTestsWithFilter($"{path}[Category!=Issue7437{scope}]",
                [result => result.ResultSummary.Counters.Total.ShouldBe(0)]);
        }
    }

    [Test]
    [Arguments("InvalidClass", "Run")]
    [Arguments("InvalidMethod", "GenericRun")]
    [Arguments("UninferredClass", "Run")]
    public async Task Discovery_Failure_Does_Not_Match_Unrelated_Filter(string className, string methodName)
    {
        var path = $"/*/TUnit.TestProject.Bugs._7437/{className}*/{methodName}";
        await RunTestsWithFilter($"{path}[Issue7437Scope=Other]",
            [result => result.ResultSummary.Counters.Total.ShouldBe(0)]);
        await RunTestsWithFilter($"{path}[Category=Issue7437Other]",
            [result => result.ResultSummary.Counters.Total.ShouldBe(0)]);
    }

    private Task AssertSelected(string filter, int expectedFailed) => RunTestsWithFilter(filter,
    [
        result => result.ResultSummary.Counters.Total.ShouldBe(1),
        result => result.ResultSummary.Counters.Failed.ShouldBe(expectedFailed),
        result => result.ResultSummary.Counters.Passed.ShouldBe(1 - expectedFailed)
    ]);
}
