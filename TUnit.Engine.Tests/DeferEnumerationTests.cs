using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Validates DeferEnumeration: a data source marked with it is NOT expanded during discovery (one
/// placeholder node is shown) and is expanded into the real cases at runtime. The run-time counts below
/// include the placeholder itself (reported once as a passed container), e.g. a 10-row source yields
/// 10 cases + 1 placeholder = 11. Discovery-time collapse is covered by manual verification / list-tests.
/// </summary>
public class DeferEnumerationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Single_Deferred_Test_Expands_To_All_Cases_At_Runtime()
    {
        await RunTestsWithFilter(
            "/*/*/DeferEnumerationTests/Deferred",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(11),
                result => result.ResultSummary.Counters.Passed.ShouldBe(11),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Deferred_Test_Honours_Repeat()
    {
        // [Repeat(2)] => 3 runs per case; 10 cases => 30 + 1 placeholder = 31.
        await RunTestsWithFilter(
            "/*/*/DeferEnumerationTests/DeferredWithRepeat",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(31),
                result => result.ResultSummary.Counters.Passed.ShouldBe(31),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Deferred_Data_Source_Error_Surfaces_At_Runtime_Without_Crashing_Discovery()
    {
        // The throwing data source must not crash discovery; the error surfaces as a failed result.
        await RunTestsWithFilter(
            "/*/*/DeferEnumerationErrorTests/*",
            [
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }
}
