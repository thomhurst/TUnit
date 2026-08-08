using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class FailureSignalTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task CallbackFailuresAreReportedByTheTestRunner()
    {
        await RunTestsWithFilter(
            "/*/*/FailureSignalTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(3),
                result =>
                {
                    var messages = string.Join(
                        Environment.NewLine,
                        result.Results.Select(x => x.Output?.ErrorInfo?.Message));

                    messages.ShouldContain("Failure reported from callback");
                    messages.ShouldContain("Failure reason reported from callback");
                    messages.ShouldContain("Non-cooperative body failed");
                }
            ]);
    }

    [Test]
    public async Task FailureSignalsAreScopedToRetryAttempts()
    {
        await RunTestsWithFilter(
            "/*/*/FailureSignalRetryTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task FailureSignalsComposeWithCustomTestExecutors()
    {
        await RunTestsWithFilter(
            "/*/*/FailureSignalCustomExecutorTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result =>
                {
                    var messages = string.Join(
                        Environment.NewLine,
                        result.Results.Select(x => x.Output?.ErrorInfo?.Message));

                    messages.ShouldContain("Failure reported through custom executor");
                    messages.ShouldContain("Failure reported before executor cleanup");
                    messages.ShouldContain("Custom executor cleanup failed");
                    messages.ShouldNotContain("The custom test executor did not complete after signal cancellation");
                }
            ]);
    }

    [Test]
    public async Task ReportsAfterTheTestBodyCompletesAreIgnoredWithCustomTestExecutors()
    {
        await RunTestsWithFilter(
            "/*/*/FailureSignalCustomExecutorLateReportTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
