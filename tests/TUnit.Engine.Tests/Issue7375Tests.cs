using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7375Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task NonOwning_Scopes_And_Partially_Resolved_Services_Are_Disposed()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7375/*/*",
            [
                // Run the complete fixture namespace: observers depend on its static records.
                // Update totals when adding cases; one resolution deliberately fails.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(10),
                result => result.ResultSummary.Counters.Passed.ShouldBe(9),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }
}
