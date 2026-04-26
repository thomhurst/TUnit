using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue5753Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task ReflectionPropertyInjection_DoesNotOverwriteExistingValue()
    {
        await RunTestsWithFilter(
            "/*/*/Issue5753ReflectionPropertyInjectionTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
