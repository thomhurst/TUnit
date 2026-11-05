using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7624Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Concrete_Params_Arrays_Preserve_Generic_Inference()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7624/ConcreteParamsArrayTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Expanded_Concrete_Params_Preserve_Constraint_Validation()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7624/ConcreteParamsConstraintTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").Output!.ErrorInfo!.Message!
                    .ShouldContain("Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'")
            ]);
    }
}
