using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

// Regression for https://github.com/thomhurst/TUnit/issues/1327: a TRX ErrorInfo only carries a
// message and a stack trace, so the inner-exception chain has to be folded into both or the
// report shows just the outermost exception.
public class NestedExceptionTrxTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Trx_ErrorInfo_Includes_Every_Inner_Exception()
    {
        await RunTestsWithFilter(
            "/*/*/NestedExceptionTests/Test",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result =>
                {
                    var errorInfo = result.Results.Single().Output?.ErrorInfo;
                    errorInfo.ShouldNotBeNull();

                    errorInfo.Message.ShouldContain("Thrown from Method1");
                    errorInfo.Message.ShouldContain("System.ArgumentException: Thrown from Method2");
                    errorInfo.Message.ShouldContain("System.InvalidOperationException: Thrown from Method3");

                    errorInfo.StackTrace.ShouldContain("NestedExceptionTests.Method1()");
                    errorInfo.StackTrace.ShouldContain("NestedExceptionTests.Method2()");
                    errorInfo.StackTrace.ShouldContain("NestedExceptionTests.Method3()");
                },
            ]);
    }

    [Test]
    public async Task Trx_ErrorInfo_Includes_Every_Aggregate_Member()
    {
        // The console host wraps the AggregateException in TestFailedException before the TRX
        // property is built; the fold must still list every member, not just the first.
        await RunTestsWithFilter(
            "/*/*/IdeExceptionReportingTests/AggregateFailures",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result =>
                {
                    var errorInfo = result.Results.Single().Output?.ErrorInfo;
                    errorInfo.ShouldNotBeNull();

                    errorInfo.Message.ShouldContain("System.InvalidOperationException: First failure");
                    errorInfo.Message.ShouldContain("System.ArgumentException: Second failure");
                    errorInfo.Message.ShouldContain("System.FormatException: Second failure cause");

                    errorInfo.StackTrace.ShouldContain("IdeExceptionReportingTests.First()");
                    errorInfo.StackTrace.ShouldContain("IdeExceptionReportingTests.Second()");
                },
            ]);
    }

    [Test]
    public async Task Trx_ErrorInfo_Includes_Every_Aggregate_Member_With_Detailed_StackTrace()
    {
        // With --detailed-stacktrace the console host reports the raw AggregateException instead of
        // the TestFailedException wrapper; it must still be reported whole rather than reduced to
        // its first member.
        await RunTestsWithFilter(
            "/*/*/IdeExceptionReportingTests/AggregateFailures",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result =>
                {
                    var errorInfo = result.Results.Single().Output?.ErrorInfo;
                    errorInfo.ShouldNotBeNull();

                    errorInfo.Message.ShouldContain("System.InvalidOperationException: First failure");
                    errorInfo.Message.ShouldContain("System.ArgumentException: Second failure");
                    errorInfo.Message.ShouldContain("System.FormatException: Second failure cause");

                    errorInfo.StackTrace.ShouldContain("IdeExceptionReportingTests.First()");
                    errorInfo.StackTrace.ShouldContain("IdeExceptionReportingTests.Second()");
                },
            ],
            new RunOptions().WithArgument("--detailed-stacktrace"));
    }
}
