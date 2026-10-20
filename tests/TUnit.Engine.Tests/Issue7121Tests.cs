using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7121: a generic test whose type parameter occurs more
/// than once must run with the same type argument reflection mode reports.
/// </summary>
public class Issue7121Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Repeated_Type_Parameter_Runs_With_Reported_Type()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7121/RepeatedTypeParameterTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.Results.Single().TestName.ShouldStartWith("Repeated<int>")
            ]);
    }
}
