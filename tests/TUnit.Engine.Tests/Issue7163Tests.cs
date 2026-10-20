using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A generic test method whose type arguments its data source's declared type cannot determine is
/// discovered in both modes. Reflection mode infers them from the row values and runs the rows.
/// Source-generated mode cannot, so it reports each row as a failed test that explains the fix,
/// rather than registering no test at all (#7163).
/// </summary>
public class Issue7163Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string Namespace = "/*/TUnit.TestProject.Bugs._7163";
    private const string CannotInfer = "Could not infer the type arguments of generic test method 'Generic' from data source [MethodDataSource(nameof(Rows))]";

    [Test]
    [Arguments("ObjectRowsTests", 2)]
    [Arguments("ObjectArrayRowsTests", 1)]
    public async Task Uninferable_Rows_Are_Not_Dropped(string className, int rows)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}/*",
            testMode == TestMode.Reflection
                ?
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(rows),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(rows),
                ]
                :
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(rows),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(rows),
                    result => result.Results.ShouldAllBe(x => x.TestName == "Generic"),
                    result => result.Results.ShouldAllBe(x => x.Output!.ErrorInfo!.Message!.Contains(CannotInfer)),
                ]);
    }

    [Test]
    public async Task Inferable_Row_Runs_Beside_Uninferable_Source()
    {
        await RunTestsWithFilter(
            $"{Namespace}/PartlyInferableTests/*",
            testMode == TestMode.Reflection
                ?
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(2),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                ]
                :
                [
                    result => result.ResultSummary.Counters.Total.ShouldBe(2),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                    result => result.Results.Single(x => x.Outcome == "Passed").TestName.ShouldBe("Generic<int>(5)"),
                    result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message.ShouldContain(CannotInfer),
                ]);
    }

    [Test]
    [Arguments("GenerateGenericTestWithObjectRowsTests")]
    [Arguments("NullableGenericReturnTypeTests")]
    public async Task GenerateGenericTest_Runs_In_Both_Modes(string className)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            ]);
    }

    [Test]
    [Arguments("ArgumentsAndDataSourceWithSameTypesTests")]
    [Arguments("TwoDataSourcesWithSameTypesTests")]
    public async Task Sources_Inferring_The_Same_Types_All_Run_In_Both_Modes(string className)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
            ]);
    }

    [Test]
    [Arguments("NoDataSourceTests")]
    [Arguments("NestedGenericReturnTypeTests")]
    [Arguments("ContainingGenericReturnTypeTests")]
    public async Task Method_Without_Data_Source_Fails_In_Both_Modes(string className)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single().TestName.ShouldBe("Generic"),
            ]);
    }
}
