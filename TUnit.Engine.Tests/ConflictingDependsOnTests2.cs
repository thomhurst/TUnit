using FluentAssertions;

namespace TUnit.Engine.Tests;

public class ConflictingDependsOnTests2 : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ConflictingDependsOnTests2/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(3),
                result => result.ResultSummary.Counters.Passed.Should().Be(0),
                result => result.ResultSummary.Counters.Failed.Should().Be(3),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                result => result.Results.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test1 > Test3 > Test2 > Test1"),
                result => result.Results.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test2 > Test1 > Test3 > Test2"),
                result => result.Results.First(x => x.TestName == "Test3").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test3 > Test2 > Test1 > Test3"),
            ]);
    }
}