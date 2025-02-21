using Shouldly;

namespace TUnit.Engine.Tests;

public class ArgumentWithImplicitConverterTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ArgumentWithImplicitConverterTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}