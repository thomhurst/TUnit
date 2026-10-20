using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// An untyped [Arguments(null)] method row on a generic class with nothing else to supply the class's
/// type argument infers object and runs, or fails visibly when object breaks the class's constraints,
/// instead of silently producing no test, in both modes (#7141).
/// </summary>
public class Issue7141Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string Namespace = "/*/TUnit.TestProject.Bugs._7141";

    [Test]
    [Arguments("NullRowOnGenericClass", 1)]
    [Arguments("NullAndValueRowsOnGenericClass", 2)]
    [Arguments("GenerateGenericTestWithNullRow", 1)]
    public async Task Null_Row_Runs(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            // Trailing '*': generic class nodes carry the arity
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Null_Row_That_Breaks_The_Class_Constraint_Fails()
    {
        await RunTestsWithFilter(
            $"{Namespace}/NullRowOnConstrainedGenericClass*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message
                    .ShouldContain("Type 'Object' does not satisfy the 'struct' constraint for generic parameter 'T'")
            ]);
    }
}
