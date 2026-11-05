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

    [Test]
    [Arguments("*InvalidPrivateAttributeClass", "Issue7437Private", 1)]
    [Arguments("InvalidConstructorClass", "Issue7437Constructor", 2)]
    [Arguments("InvalidUnusedSourceClass", "Issue7437UnusedSource", 1)]
    public Task Failure_Attributes_Do_Not_Replace_Constraint_Errors(string className, string category, int count) =>
        RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7437/{className}*/Run[Category={category}]",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(count),
            result => result.ResultSummary.Counters.Failed.ShouldBe(count),
            result => result.Results.Where(x => x.Outcome == "Failed").All(x =>
                x.Output?.ErrorInfo?.Message?.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true).ShouldBeTrue()
        ]);

    private Task AssertSelected(string filter, int expectedFailed) => RunTestsWithFilter(filter,
    [
        result => result.ResultSummary.Counters.Total.ShouldBe(1),
        result => result.ResultSummary.Counters.Failed.ShouldBe(expectedFailed),
        result => result.ResultSummary.Counters.Passed.ShouldBe(1 - expectedFailed)
    ]);
}
