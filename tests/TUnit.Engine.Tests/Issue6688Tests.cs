using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue6688Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Timeout_Preserves_Custom_Cancellation_Message()
    {
        await RunTestsWithFilter(
            "/*/*/TimeoutCancellationExceptionTests/Custom_Cancellation_Message",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Timeout.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("Failed due to XYZ"),
            ],
            new RunOptions().WithArgument("--detailed-stacktrace"));
    }

    [Test]
    public async Task Timeout_Preserves_Custom_Non_Cancellation_Exception_Message()
    {
        await RunTestsWithFilter(
            "/*/*/TimeoutCancellationExceptionTests/Custom_Non_Cancellation_Exception_Message",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Timeout.ShouldBe(1),
                result => result.Results.Single().Output?.ErrorInfo?.Message.ShouldContain("Custom non-cancellation diagnostic"),
            ],
            new RunOptions().WithArgument("--detailed-stacktrace"));
    }
}
