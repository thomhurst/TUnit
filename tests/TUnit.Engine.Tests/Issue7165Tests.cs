using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Test methods and classes with <c>dynamic</c> parameters and properties compile in source-generated mode
/// and receive their data in every mode (#7165).
/// </summary>
public class Issue7165Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Dynamic_Parameters_Receive_Their_Data()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7165/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(5),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
