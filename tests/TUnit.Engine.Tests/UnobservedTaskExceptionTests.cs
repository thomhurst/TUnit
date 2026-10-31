using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Engine.Tests.Scheduling;

namespace TUnit.Engine.Tests;

/// <summary>
/// #7288: an unobserved task exception is reported in the session results instead of only in raw
/// console output, and --fail-on-unobserved-task-exceptions turns it into a failed result.
/// </summary>
public class UnobservedTaskExceptionTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string Filter = "/*/*/UnobservedTaskException7288/*";
    private const string ExpectedMessage = "Intentional unobserved task exception (#7288)";

    [Test]
    public async Task Without_Option_Run_Passes_And_Reports_Exception()
    {
        var process = new ProcessResultCapture();

        await RunTestsWithFilter(Filter,
        [
            _ => process.AssertEndedWithoutCrash(0),
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            _ =>
            {
                var output = process.Result!.StandardOutput;
                output.ShouldContain("1 unobserved task exception occurred during the test session.");
                output.ShouldContain(ExpectedMessage);
            },
        ],
        new RunOptions().Capture(process));
    }

    [Test]
    public async Task With_Option_Run_Fails_With_Exception_As_Result()
    {
        var process = new ProcessResultCapture();

        await RunTestsWithFilter(Filter,
        [
            _ => process.AssertEndedWithoutCrash(2),
            result => result.ResultSummary.Outcome.ShouldBe("Failed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result =>
            {
                var node = result.Results.Single(x => x.TestName!.StartsWith("Unobserved task exception", StringComparison.Ordinal));
                node.Outcome.ShouldBe("Failed");
                node.TestName.ShouldBe($"Unobserved task exception - InvalidOperationException: {ExpectedMessage}");
                node.Output?.ErrorInfo?.Message.ShouldNotBeNull().ShouldContain(ExpectedMessage);
            },
        ],
        new RunOptions()
            .WithArgument("--fail-on-unobserved-task-exceptions")
            .Capture(process));
    }
}
