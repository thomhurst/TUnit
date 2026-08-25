using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue6670Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Hidden_Generic_Interface_Method_Mock_Works()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._6670/Issue6670MockTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
