using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Engine tests that validate parallelism works correctly across different execution modes.
/// Invokes TUnit.TestProject.ParallelismValidationTests to ensure:
/// 1. Tests without constraints run in parallel
/// 2. ParallelLimiter correctly limits concurrency
/// 3. Different parallel limiters work independently
/// </summary>
public class ParallelismValidationEngineTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task UnconstrainedParallelTests_ShouldRunInParallel()
    {
        await RunTestsWithFilter("/*/*/ParallelismValidationTests.UnconstrainedParallelTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(12), // 4 tests × 3 repeats
            result => result.ResultSummary.Counters.Passed.ShouldBe(12),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task LimitedParallelTests_ShouldRespectLimit()
    {
        await RunTestsWithFilter("/*/*/ParallelismValidationTests.LimitedParallelTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(12), // 4 tests × 3 repeats
            result => result.ResultSummary.Counters.Passed.ShouldBe(12),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task StrictlySerialTests_ShouldRunOneAtATime()
    {
        await RunTestsWithFilter("/*/*/ParallelismValidationTests.StrictlySerialTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(8), // 4 tests × 2 repeats
            result => result.ResultSummary.Counters.Passed.ShouldBe(8),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task HighParallelismTests_ShouldAllowHighConcurrency()
    {
        await RunTestsWithFilter("/*/*/ParallelismValidationTests.HighParallelismTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(12), // 4 tests × 3 repeats
            result => result.ResultSummary.Counters.Passed.ShouldBe(12),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task AllParallelismTests_ShouldPassTogether()
    {
        // Run all parallelism validation tests together to ensure they don't interfere
        await RunTestsWithFilter("/*/*/ParallelismValidationTests.*/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(44), // 12 + 12 + 8 + 12
            result => result.ResultSummary.Counters.Passed.ShouldBe(44),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }
}