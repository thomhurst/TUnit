using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7077Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Skip_With_Empty_Or_Null_Reason_Skips_The_Test()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7077/EmptySkipReasonTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(4)
            ]);
    }

    [Test]
    public async Task Class_Level_Skip_With_Empty_Reason_Skips_Without_Constructing()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7077/EmptySkipReasonClassTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }
}
