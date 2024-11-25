using FluentAssertions;

namespace TUnit.Engine.Tests;

public class MatrixTests1 : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/MatrixTests/MatrixTest_One",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(24, "Total"),
                result => result.ResultSummary.Counters.Passed.Should().Be(24, "Passed"),
                result => result.ResultSummary.Counters.Failed.Should().Be(0, "Failed"),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0, "Skipped")
            ]);
    }
}