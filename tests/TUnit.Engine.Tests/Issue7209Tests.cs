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
    [Arguments("TypeFreeClassRowOverloadedConstructors", 2)]
    [Arguments("TypeFreeClassRowPrimaryConstructorBesideTypedConstructor", 2)]
    [Arguments("TypeFreeClassRowTestConstructorTakesTypeParameter", 1)]
    [Arguments("TypeFreeClassRowTestConstructorTakesNoTypeParameter", 2)]
    [Arguments("TypeFreeClassRowWithUninferredMethodSource", 3)]
    [Arguments("EmptyClassRow", 2)]
    [Arguments("ConstrainedClassWithTypeFreeClassRowAndLeadingToken", 1)]
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

    [Test]
    public async Task Null_Row_That_Breaks_The_Class_Constraint_Beside_A_Type_Free_Class_Row_Fails()
    {
        await RunTestsWithFilter(
            $"{Namespace}/ConstrainedClassWithTypeFreeClassRow*/*",
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
