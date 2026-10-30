using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Verifies issue #7290: a property-injected data source that implements
/// ITestRegisteredEventReceiver receives OnTestRegistered, so it can set a parallel limiter or
/// skip the test. Runs every class in Bugs/_7290/Tests.cs: four repeated property consumers, one
/// constructor-plus-property consumer and one test skipped by its injected fixture.
/// </summary>
public class RegisteredReceiverInjectedProperty7290Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7290/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(5),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }
}
