using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Verifies the partial-injection-failure edge of issue #6554: when one injected property's
/// data source throws during registration, event receivers on sibling properties that DID
/// resolve must still fire. Runs both classes in Bugs/_6554/PartialInjectionFailureTests.cs:
/// the registration failure itself plus the observer test that asserts OnTestEnd ran.
/// </summary>
public class PartialInjectionFailure6554Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/PartialInjectionFailure*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
