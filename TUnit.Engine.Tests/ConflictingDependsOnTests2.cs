using Shouldly;

namespace TUnit.Engine.Tests;

public class ConflictingDependsOnTests2 : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ConflictingDependsOnTests2/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(3),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => result.Results.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: Test1 > Test3 > Test2 > Test1"),
                result => result.Results.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: Test2 > Test1 > Test3 > Test2"),
                result => result.Results.First(x => x.TestName == "Test3").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: Test3 > Test2 > Test1 > Test3"),
            ]);
    }
}