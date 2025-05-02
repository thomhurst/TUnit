using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

public class TestDiscoveryAfterHookTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter("/*/*/TestDiscoveryAfterTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                _ => FindFile(x => x.Name.StartsWith("TestDiscoveryAfterTests") && x.Extension == ".txt").AssertExists()
            ]);
    }
}
