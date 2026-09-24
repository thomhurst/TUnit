using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class UnhandledExceptionTrxTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Trx_Preserves_Test_Results_And_Shared_Resource_Disposal_Error()
    {
        await RunTestsWithFilter(
            "/*/*/UnhandledExceptionTrxTestCases/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.Single(x => x.TestName == "PassingTest").Outcome.ShouldBe("Passed"),
                result =>
                {
                    var failedTest = result.Results.Single(x => x.TestName == "FailingTest");
                    failedTest.Outcome.ShouldBe("Failed");
                    var errorInfo = failedTest.Output?.ErrorInfo;
                    errorInfo.ShouldNotBeNull();
                    errorInfo.Message.ShouldContain("Test body failure");
                    errorInfo.StackTrace.ShouldContain("UnhandledExceptionTrxTestCases.FailingTest()");
                },
                result =>
                {
                    var cleanup = result.Results.Single(x => x.TestName!.StartsWith("Unhandled exception -", StringComparison.Ordinal));
                    cleanup.Outcome.ShouldBe("Failed");
                    var errorInfo = cleanup.Output?.ErrorInfo;
                    errorInfo.ShouldNotBeNull();
                    errorInfo.Message.ShouldContain("Shared resource disposal failure");
                    errorInfo.StackTrace.ShouldContain("ThrowingResource.DisposeAsync()");
                }
            ],
            new RunOptions().WithArgument("--detailed-stacktrace"));
    }
}
