using FluentAssertions;

namespace TUnit.Engine.Tests;

public class ConcreteBasedOnAbstractClassTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/(ConcreteClass1|ConcreteClass2)/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(3),
                result => result.ResultSummary.Counters.Passed.Should().Be(2),
                result => result.ResultSummary.Counters.Failed.Should().Be(1),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}