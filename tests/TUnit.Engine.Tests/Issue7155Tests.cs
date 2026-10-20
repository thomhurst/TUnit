using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A null and a value-type value for the same type parameter of a generic test method infer object in
/// both modes, instead of the value type with the null turned into its default (#7155).
/// </summary>
public class Issue7155Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("NullAndValueTypeGenericMethod", 2)]
    [Arguments("NullAndValueTypeGenerateGenericTestMethod", 1)]
    public async Task Null_And_Value_Type_Row_Runs_As_Object(string className, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7155/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
