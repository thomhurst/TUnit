using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7224Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Method_Sources_Infer_Generic_Class_Types()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7224/*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(31),
            result => result.ResultSummary.Counters.Passed.ShouldBe(31),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task Uninferable_Source_Is_Reported_And_Explicit_Types_Still_Run()
    {
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7224Unsupported/*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(6),
            result => result.ResultSummary.Counters.Passed.ShouldBe(1),
            result => result.ResultSummary.Counters.Failed.ShouldBe(5)
        ]);
    }
}
