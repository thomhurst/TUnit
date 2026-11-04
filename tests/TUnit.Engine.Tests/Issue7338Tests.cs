using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7338Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Final_Dependency_Skip_Completes_Assembly_And_Session()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7338/DependencySkippedCleanupTests/*",
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
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7338/*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(3),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
        ]);
    }
}
