using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A row of a generic test that leaves out trailing optional parameters runs with their declared
/// defaults in both modes, instead of failing with IndexOutOfRangeException in source-gen mode (#7146).
/// </summary>
public class Issue7146Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Omitted_Optionals_Of_Generic_Tests_Take_Their_Defaults()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7146/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
