using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// The rows of a class-level data source whose row types are known only at run time run on the generic class
/// instantiation that their values infer, in both modes (#7210).
/// </summary>
public class Issue7210Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("ArgumentsRowAndMixedSource", 3)]
    [Arguments("MixedSourceOnly", 2)]
    [Arguments("RowsSplitAcrossSources", 2)]
    [Arguments("WrappedRowInfersItsValue", 2)]
    public async Task Rows_Run_On_The_Instantiation_They_Infer(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7210/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("EmptySourceWithSkipIfEmpty", 1)]
    [Arguments("CustomSourceSettingsKept", 2)]
    public async Task Rows_Keep_Their_Source_Skip_Settings(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7210/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(expectedTests)
            ]);
    }

    [Test]
    public async Task Method_Row_Type_That_No_Class_Row_Infers_Fails()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7210/MethodRowTypeNoClassRowInfers*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results
                    .Single(x => x.Outcome == "Failed")
                    .Output?.ErrorInfo?.Message
                    .ShouldContain("none of the class-level data rows infer")
            ]);
    }
}
