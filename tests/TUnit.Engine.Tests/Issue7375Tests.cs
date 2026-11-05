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
                // One resolution fails deliberately; its observer verifies the earlier service's cleanup.
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(8),
                result => result.ResultSummary.Counters.Passed.ShouldBe(7),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }
}
