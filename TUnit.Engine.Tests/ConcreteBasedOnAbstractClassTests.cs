using Shouldly;

namespace TUnit.Engine.Tests;

public class ConcreteBasedOnAbstractClassTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/(ConcreteClass1|ConcreteClass2)/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}