using FluentAssertions;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

public class InheritedPropertySetterTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/InheritedPropertySetterTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                _ => File.ReadAllText(FindFile(x => x.Name == "InheritedPropertySetterTests_CapturedOutput.txt").AssertExists().FullName).Should().Contain(
                    """
                    Initializing Static Property
                    Before Test Session
                    Before Assembly
                    Before Class
                    Initializing Property
                    Initializing Property
                    Initializing Property
                    Initializing Property
                    Running Test
                    StaticInnerModel { IsInitialized = True, Foo = Bar }
                    Disposing Property
                    """
                    ),
                _ => FindFile(x => x.Name == "StaticProperty_IAsyncDisposable.txt").AssertExists()
            ]);
    }
}