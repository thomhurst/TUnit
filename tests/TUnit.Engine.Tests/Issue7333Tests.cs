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
            result => result.ResultSummary.Counters.Total.ShouldBe(44),
            result => result.ResultSummary.Counters.Passed.ShouldBe(42),
            result => result.ResultSummary.Counters.Failed.ShouldBe(2),
            result => result.Results.Where(test => test.Outcome == "Failed" && !test.TestName!.StartsWith("ReportsMissingConstructorValue", StringComparison.Ordinal))
                .ShouldAllBe(test => test.Output!.ErrorInfo!.Message!.Contains("type arguments"))
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
}
