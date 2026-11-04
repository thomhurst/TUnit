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
            result => result.ResultSummary.Counters.Total.ShouldBe(44),
            result => result.ResultSummary.Counters.Passed.ShouldBe(44),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task Supplied_Constructor_Array_Infers_Alongside_Explicit_Class_Types()
    {
        // Standalone constructor-row inference in generated mode is tracked in #7333.
        Skip.When(testMode != TestMode.Reflection, "Only reflection mode infers standalone constructor rows");
        await RunTestsWithFilter("/*/TUnit.TestProject.Bugs._7224Reflection/*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(2),
            result => result.ResultSummary.Counters.Passed.ShouldBe(2),
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
