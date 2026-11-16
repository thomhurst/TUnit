using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A custom data source's DisplayName, Skip and Categories properties apply to its rows in every mode,
/// including Native AOT, where the trimmer removes getters that only the engine reads (#7782).
/// </summary>
public class Issue7782Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Method_Rows_Take_Their_Custom_Source_Settings()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7782/CustomDataSourceRowSettings/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(11),
                result => result.ResultSummary.Counters.Passed.ShouldBe(10),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Class_Rows_Take_Their_Custom_Source_Settings()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7782/CustomClassDataSourceRowSettings/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
