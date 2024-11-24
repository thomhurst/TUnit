using FluentAssertions;

namespace TUnit.Engine.Tests.Bugs;

public class Bug1187 : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._1187/*/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(24),
                result => result.ResultSummary.Counters.Passed.Should().Be(24),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}