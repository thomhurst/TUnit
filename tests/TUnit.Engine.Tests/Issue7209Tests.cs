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
    [Arguments("ClassRowInfersNoTypeArgument", 2, 2)]
    [Arguments("ClassRowInfersTypeArgument", 1, 1)]
    [Arguments("TypeFreeClassRowBesideOtherSource", 2, 1)]
    [Arguments("TypeFreeClassRowWithUninferredMethodSource", 2, 2)]
    [Arguments("TypeFreeClassRowWithInferringMethodSource", 1, 1)]
    [Arguments("TypeFreeClassRowWithInferringRowAndMethodSource", 2, 2)]
    [Arguments("TypeFreeClassRowWithCancellationToken", 1, 1)]
    [Arguments("TypeFreeClassRowWithTypedMethodSource", 1, 1)]
    [Arguments("TypeFreeClassRowWithInferringParameterAttribute", 2, 2)]
    [Arguments("TypeFreeClassRowWithInferringRowAndTypedSource", 3, 2)]
    [Arguments("TypeFreeClassRowWithTwoInferringMethodSources", 4, 2)]
    [Arguments("TypeFreeClassRowWithTypedSourceAndInferringParameterAttribute", 4, 3)]
    [Arguments("TypeFreeClassRowWithTwoTypedSources", 2, 1)]
    public async Task Rows_Run_On_The_Instantiation_They_Infer(string className, int expectedTests, int expectedDistinctNames)
    {
        await RunTestsWithFilter(
            // Trailing '*': generic class nodes carry the arity
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                // Distinct method rows keep distinct names, so a row running twice in place of another fails
                result => result.Results.Select(x => x.TestName).Distinct(StringComparer.Ordinal).Count().ShouldBe(expectedDistinctNames)
            ]);
    }

    [Test]
    public async Task Method_Source_Infers_Beside_A_Type_Free_Class_Row_And_An_Empty_Class_Source()
    {
        // C<int> from the method source, built from the class row, plus the empty source's skipped test
        await RunTestsWithFilter(
            $"{Namespace}/TypeFreeClassRowBesideEmptySourceWithInferringMethodSource*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("TypeFreeClassRowConstraintViolation", "Type 'Object' does not satisfy the 'struct' constraint for generic parameter 'T'")]
    [Arguments("TypeFreeClassRowArrayConstraintViolation", "Type 'Int32' does not satisfy the 'class' constraint for generic parameter 'T'")]
    public async Task Method_Row_Inferring_An_Invalid_Class_Type_Beside_A_Type_Free_Class_Row_Fails(string className, string expectedMessage)
    {
        await RunTestsWithFilter(
            $"{Namespace}/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").TestName.ShouldBe("Run"),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message
                    .ShouldContain(expectedMessage)
            ]);
    }
}
