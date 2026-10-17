using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Every class-level and method-level [Arguments] row on a generic test class produces its own test,
/// whatever type it infers, in both modes (#7106).
/// </summary>
public class Issue7106Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("GenericClassSameTypeRows", 3)]
    [Arguments("GenericClassMixedTypeRows", 4)]
    [Arguments("GenericClassSameTypeMethodRows", 2)]
    [Arguments("GenericClassMixedTypeMethodRows", 3)]
    [Arguments("GenericClassRowsTimesGenericMethodRows", 4)]
    [Arguments("GenericClassRowsTimesMethodRows", 4)]
    [Arguments("GenericClassRowWithMethodRow", 1)]
    public async Task Every_Row_Runs(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7106/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
