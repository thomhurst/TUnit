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
            result => result.ResultSummary.Counters.Total.ShouldBe(74),
            result => result.ResultSummary.Counters.Passed.ShouldBe(66),
            result => result.ResultSummary.Counters.Failed.ShouldBe(8),
            result => result.Results.Where(test => test.Outcome == "Failed" && !test.TestName!.StartsWith("ReportsMissingConstructorValue", StringComparison.Ordinal))
                .ShouldAllBe(test => test.Output!.ErrorInfo!.Message!.Contains("type arguments")
                    || test.Output.ErrorInfo.Message.Contains("does not satisfy the 'struct' constraint"))
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
    [Arguments("ExplicitExternalNullRowsWithMethodSource", 1)]
    [Arguments("ExplicitExternalOnlyNullRowsWithMethodSource", 1)]
    [Arguments("GroupedRowsWithMarkedConstructor", 2)]
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
    [Arguments("EmptyExternalConstraintRows", 1, 0)]
    [Arguments("NullExternalConstraintRows", 1, 0)]
    [Arguments("NonemptyExternalConstraintRows", 2, 1)]
    [Arguments("TupleExternalConstraintRows", 2, 1)]
    [Arguments("ObjectTupleExternalConstraintRows", 2, 1)]
    public async Task External_Constraint_Failures_Require_Actual_Rows(string className, int total, int failed)
    {
        await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7333/{className}*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(total),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(failed)
        ]);
    }

    // The same fixtures and expected class types run through generated/AOT and reflection discovery.
    [Test]
    [Arguments("ExternalSealedArrayRows")]
    [Arguments("ExternalNullableArrayParameterRows")]
    [Arguments("ExternalNestedArrayRows")]
    [Arguments("ExternalParameterlessConstructorRows")]
    [Arguments("OwnParameterlessConstructorRows")]
    [Arguments("ExplicitConstructorRows")]
    public async Task Class_Row_Type_And_Constructor_Parity(string className)
    {
        await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7333/{className}*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1)
        ]);
    }

    [Test]
    public async Task First_Public_Constructor_Must_Infer_Class_Types()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7333/FirstConstructorCannotInfer*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.Results.Single().Output!.ErrorInfo!.Message!.ShouldContain("type arguments")
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
