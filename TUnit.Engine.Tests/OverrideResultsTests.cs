using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class OverrideResultsTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/OverrideResultsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(2),
                result =>
                {
                    // Verify skip reason is included in TRX output for overridden test
                    var skippedTest = result.Results.FirstOrDefault(x => x.TestName.Contains("TestSkippedWithSpecificReason"));
                    skippedTest.ShouldNotBeNull();
                    skippedTest.Outcome.ShouldBe("NotExecuted");
                    // Check if skip reason appears in StdOut (DebugOrTraceTrxMessage may appear there)
                    var output = skippedTest.Output?.StdOut ?? "";
                    output.ShouldContain("Skipped: test-skip foo bar baz.");
                }
            ]);
    }
}
