using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class NestedClassFilteringTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Filter_NestedClass_ByFullNestedName()
    {
        await RunTestsWithFilter(
            "/*/*/NestedTestClassTests+NestedClass/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_NestedClass_SpecificMethod()
    {
        await RunTestsWithFilter(
            "/*/*/NestedTestClassTests+NestedClass/Inner",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Filter_OuterClass_StillWorks()
    {
        await RunTestsWithFilter(
            "/*/*/NestedTestClassTests/Outer",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
