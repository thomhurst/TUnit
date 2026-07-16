using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue6361Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Deferred_Instance_Method_Data_Source_Does_Not_Reuse_Enumeration_Instance()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._6361/Issue6361InstanceMethodDataSourceIsolationTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
