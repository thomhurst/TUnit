using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ExpectedStateTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Pass()
    {
        await RunTestsWithFilter(
            "/**[EngineTest=Pass]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed")
            ]);
    }

    [Test]
    public async Task Fail()
    {
        await RunTestsWithFilter(
            "/**[EngineTest=Failure]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed")
            ]);
    }
}
