using System.Globalization;
using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ExpectedStateTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Pass()
    {
        var options = new RunOptions();

        if (testMode == TestMode.Reflection && OperatingSystem.IsWindows())
        {
            // The outer engine suite's 30-minute hang detector cannot capture this child:
            // its session timeout exits at ten minutes. Capture the stalled child first (#7554).
            // Hangdump timeout measures inactivity and resets on progress. The absolute deadline
            // also triggers a dump at eight minutes, even if the child made progress earlier.
            // Disable early graceful stop so cancellation cannot hide the stalled stacks.
            options.WithArgument("--hangdump")
                .WithArgument("--hangdump-filename")
                .WithArgument("hangdump.windows-reflection-pass.dmp")
                .WithArgument("--hangdump-timeout")
                .WithArgument("8m")
                .WithEnvironmentVariable("TESTINGPLATFORM_DEADLINE",
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O", CultureInfo.InvariantCulture))
                .WithEnvironmentVariable("TESTINGPLATFORM_DEADLINE_DUMP_MARGIN", "120s")
                .WithEnvironmentVariable("TESTINGPLATFORM_DEADLINE_STOP_MARGIN", "0s");
        }

        await RunTestsWithFilter(
            "/**[EngineTest=Pass]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed")
            ], options);
    }
}
