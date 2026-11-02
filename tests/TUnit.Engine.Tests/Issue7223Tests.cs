using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A class-level [Arguments] row infers a generic class's type argument through a T[] constructor parameter, from
/// non-null and null values alike, in both modes (#7223).
/// </summary>
public class Issue7223Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("ArrayParameterClassRowInference", 1)]
    [Arguments("MixedNullAndValueArrayClassRows", 2)]
    [Arguments("ArrayParameterAfterOtherClassRowInference", 1)]
    [Arguments("NullBeforeValueClassRowInference", 1)]
    public async Task Class_Row_Infers_T_Through_Constructor_Parameter(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7223/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
