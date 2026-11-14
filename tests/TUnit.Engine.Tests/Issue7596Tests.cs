using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// An own-class row that omits an optional constructor parameter stays on the instantiation it fits, and builds
/// its instance with the parameter's default, in both modes (#7596).
/// </summary>
public class Issue7596Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Rows_Omitting_Optional_Parameters_Run_On_The_Explicit_Instantiation()
    {
        // The [Arguments] row, an enum row and a custom IConvertible row
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7596/CustomConvertibleEnumRows*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Enum_Array_Rows_Run_On_The_Explicit_Instantiation()
    {
        // The [Arguments] row, an enum array row and an int array row
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7596/EnumArrayRows*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
