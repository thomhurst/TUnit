using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Rows that omit optional parameters bind to the test method, never to a sibling overload that
/// lacks those parameters, and the omitted parameters receive their declared defaults (#7122).
/// </summary>
public class Issue7122Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Omitted_Optionals_Bind_To_The_Test_Method_With_Declared_Defaults()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7122/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(10),
                result => result.ResultSummary.Counters.Passed.ShouldBe(10),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
