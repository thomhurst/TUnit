using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A null [Arguments] value on a generic test method infers object in both modes, including
/// [Arguments(null)], which passes a null params array (#7120).
/// </summary>
public class Issue7120Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("NullArgumentsGenericMethod", 1)]
    [Arguments("NullElementGenericMethod", 1)]
    [Arguments("NullAndTypedRowsGenericMethod", 2)]
    public async Task Null_Row_Runs_As_Object(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7120/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
