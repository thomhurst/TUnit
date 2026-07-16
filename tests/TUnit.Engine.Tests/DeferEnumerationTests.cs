using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Validates DeferEnumeration: a data source marked with it is NOT expanded during discovery (one
/// placeholder node is shown) and is expanded into the real cases at runtime. The placeholder is reported
/// as a container whose result aggregates its cases, so the run-time counts below are the number of data
/// cases plus one for the placeholder (e.g. a 10-row source => 10 cases + 1 placeholder = 11). Discovery-time
/// collapse is covered by manual verification / list-tests.
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
        // [Repeat(2)] => 3 runs per case; 10 cases => 30 cases + 1 placeholder container = 31.
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
    public async Task Deferred_Class_Data_Source_Expands_At_Runtime()
    {
        // Deferral on a class-level (constructor) data source: 5 class instances x 1 method = 5 cases
        // + 1 placeholder container = 6.
        await RunTestsWithFilter(
            "/*/*/DeferEnumerationClassDataTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Deferred_Data_Source_Error_Surfaces_At_Runtime_Without_Crashing_Discovery()
    {
        // The throwing data source must not crash discovery; the error surfaces as a failed case, and the
        // placeholder container aggregates to failed too (failed case + failed container = 2).
        await RunTestsWithFilter(
            "/*/*/DeferEnumerationErrorTests/*",
            [
                result => result.ResultSummary.Counters.Failed.ShouldBe(2)
            ]);
    }
}
