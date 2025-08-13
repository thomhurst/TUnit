using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ConflictingDependsOnTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ConflictingDependsOnTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => result.Results.First(x => x.TestName.Contains("Test1")).Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: ConflictingDependsOnTests.Test1 > ConflictingDependsOnTests.Test2 > ConflictingDependsOnTests.Test1"),
            ]);
    }
}
