using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// End-to-end regression test for https://github.com/thomhurst/TUnit/issues/7249.
/// A dynamic test added with AddDynamicTest must await the Task or ValueTask its method returns.
/// </summary>
public class Bug7249AsyncRuntimeDynamicTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Async_Dynamic_Test_Failures_Are_Reported()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7249/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.Results.Where(x => x.Outcome == "Failed").Select(x => x.TestName)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ShouldBe(["Task_Fails_After_Await", "ValueTask_Fails_After_Await"])
            ]);
    }
}
