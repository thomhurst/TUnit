using Shouldly;
using TrxTools.TrxParser;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7338Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Final_Dependency_Skip_Completes_Assembly_And_Session()
    {
        await RunWithCompletionMarker("/*/TUnit.TestProject.Bugs._7338/DependencySkippedCleanupTests/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(0),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
        ]);
    }

    [Test]
    public async Task Dependency_Skip_Releases_Objects_And_Completes_Class()
    {
        await RunWithCompletionMarker("/*/TUnit.TestProject.Bugs._7338/*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(3),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
        ]);
    }

    private async Task RunWithCompletionMarker(string filter, List<Action<TestRun>> assertions)
    {
        var completionPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await RunTestsWithFilter(filter, assertions,
                new RunOptions().WithEnvironmentVariable("TUNIT_7338_COMPLETION_PATH", completionPath));
            File.Exists(completionPath).ShouldBeTrue("session completion checks must succeed");
            File.ReadAllText(completionPath).ShouldBe("completed");
        }
        finally
        {
            File.Delete(completionPath);
        }
    }
}
