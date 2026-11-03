using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7333Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Class_Method_Data_Sources_Infer_Class_Types()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Failed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(61),
            result => result.ResultSummary.Counters.Passed.ShouldBe(57),
            result => result.ResultSummary.Counters.Failed.ShouldBe(4),
            result => result.Results.Where(test => test.Outcome == "Failed" && !test.TestName!.StartsWith("ReportsMissingConstructorValue", StringComparison.Ordinal))
                .ShouldAllBe(test => test.Output!.ErrorInfo!.Message!.Contains("type arguments"))
        ]);
    }

    [Test]
    public async Task Explicit_Instantiation_With_Method_Rows_Filters_Null_Class_Rows()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/ExplicitExternalNullRowsWithMethodRows*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Passed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1)
        ]);
    }

    [Test]
    public async Task Explicit_Instantiation_With_Only_Null_Class_Rows_Retains_Test()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/ExplicitExternalOnlyNullRowsWithMethodRows*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Passed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1)
        ]);
    }

    [Test]
    public async Task Method_Only_Instantiation_Retains_Empty_Class_Row()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/MethodOnlyEmptyClassRows*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Failed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1)
        ]);
    }

    [Test]
    [Arguments("MixedPartitionNullRows", 1)]
    [Arguments("MixedPartitionArgumentsRows", 2)]
    [Arguments("MixedPartitionEmptyRows", 1)]
    [Arguments("ExplicitConstructorMappedArguments", 2)]
    [Arguments("ExplicitDeferredOwnNullRows", 2)]
    [Arguments("ExplicitExternalNullRowsWithGenericMethod", 1)]
    [Arguments("ExplicitUninferableRows", 1)]
    public async Task Explicit_Class_Rows_Preserve_Runtime_Groups(string className, int count)
    {
        await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7333/{className}*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Passed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(count),
            result => result.ResultSummary.Counters.Passed.ShouldBe(count)
        ]);
    }

    [Test]
    public async Task Matching_Method_Instantiation_Retains_Empty_Class_Row()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/MatchingMethodOnlyEmptyClassRows*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Failed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1)
        ]);
    }

    [Test]
    public async Task Empty_Source_Does_Not_Reuse_Unrelated_Class_Arguments()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/UnmatchedClassArgumentsWithEmptySource*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.Results.Single(test => test.Outcome == "Failed")
                .Output!.ErrorInfo!.Message!.ShouldContain("no class row supplies the constructor arguments")
        ]);
    }

}
