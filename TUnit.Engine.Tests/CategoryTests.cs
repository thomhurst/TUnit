using FluentAssertions;

namespace TUnit.Engine.Tests;

public class CategoryTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/CategoryTests/*[Category=A]",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(2),
                result => result.ResultSummary.Counters.Passed.Should().Be(2),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
        
        await RunTestsWithFilter(
            "/*/*/CategoryTests/*[Category=B]",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(2),
                result => result.ResultSummary.Counters.Passed.Should().Be(2),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
        
        await RunTestsWithFilter(
            "/*/*/CategoryTests/*[(Category=A)&(Category=B)]",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}