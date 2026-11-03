using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7445Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Factory_Objects_Are_Fresh_For_Each_Method_Row()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7445/FreshFactoryRows*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(2),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ]);
    }

    [Test]
    [Arguments("SyncNullCollection")]
    [Arguments("TaskNullCollection")]
    [Arguments("ValueTaskNullCollection")]
    [Arguments("AsyncNullCollection")]
    [Arguments("NullElement")]
    public async Task Null_Collections_Add_No_Row_But_Null_Elements_Remain(string className)
    {
        await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7445/{className}*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(1),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ]);
    }
}
