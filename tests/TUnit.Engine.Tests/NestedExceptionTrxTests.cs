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
}
