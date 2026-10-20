using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A generic class's instance is built from its class-level data, never from a method-level [Arguments]
/// row, when its type arguments come from a class-level data source or [GenerateGenericTest], in both
/// modes (#7161).
/// </summary>
public class Issue7161Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("ClassMethodDataSourceWithMethodRows", 2)]
    [Arguments("GenerateGenericTestTypeOnlyMethodRowsInfer", 2)]
    public async Task Instance_Is_Built_From_Class_Data(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7161/{className}*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
