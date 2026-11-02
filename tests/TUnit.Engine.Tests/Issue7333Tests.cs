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
            result => result.ResultSummary.Counters.Total.ShouldBe(17),
            result => result.ResultSummary.Counters.Passed.ShouldBe(16),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.Results.Where(test => test.Outcome == "Failed")
                .ShouldAllBe(test => test.Output!.ErrorInfo!.Message!.Contains("type arguments"))
        ]);
    }
}
