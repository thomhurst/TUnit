using Shouldly;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

public class InheritedPropertySetterTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/InheritedPropertySetterTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                _ => File.ReadAllText(FindFile(x => x.Name == "InheritedPropertySetterTests_CapturedOutput.txt").AssertExists().FullName).ShouldContain(
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