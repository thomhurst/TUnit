using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A class [Arguments] row whose values go to no type parameter supplies no class type argument, so the
/// method rows infer it, and the instance is still built from the class row, in both modes (#7209).
/// </summary>
public class Issue7209Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string Namespace = "/*/TUnit.TestProject.Bugs._7209";

    [Test]
    [Arguments("ClassRowInfersNoTypeArgument", 2)]
    [Arguments("ClassRowInfersTypeArgument", 1)]
    public async Task Rows_Run_On_The_Instantiation_They_Infer(string className, int expectedTests)
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
}
