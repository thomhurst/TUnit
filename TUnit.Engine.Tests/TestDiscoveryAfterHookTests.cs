using FluentAssertions;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

public class TestDiscoveryAfterHookTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter("/*/*/TestDiscoveryAfterTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                _ => FindFile(x => x.Name.StartsWith("TestDiscoveryAfterTests") && x.Extension == ".txt").AssertExists()
            ]);
    }
}
