using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class TimeoutTests1(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task LinkedTokensAreReportedAsTimeouts()
    {
        await RunTestsWithFilter(
            "/*/*/LinkedTokenTimeoutTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(3),
                result =>
                {
                    foreach (var test in result.Results)
                    {
                        test.Output!.ErrorInfo!.Message.ToLowerInvariant().ShouldContain("timed out");
                    }

                    result.Results.Single(test => test.TestName.Contains("CustomLinkedCancellationDiagnostic"))
                        .Output!.ErrorInfo!.Message.ShouldContain("Linked timeout diagnostic");
                }
            ]);
    }

    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/TimeoutCancellationTokenTests/BasicTest",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeLessThan(TimeSpan.FromMinutes(1)),
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeGreaterThan(TimeSpan.FromSeconds(4)),
            ]);
    }
}
