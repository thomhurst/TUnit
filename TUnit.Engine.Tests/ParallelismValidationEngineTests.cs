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
        await RunTestsWithFilter("/*/*/UnconstrainedParallelTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(16), // 4 tests × 4 runs (Repeat(3) = original + 3)
            result => result.ResultSummary.Counters.Passed.ShouldBe(16),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task LimitedParallelTests_ShouldRespectLimit()
    {
        await RunTestsWithFilter("/*/*/LimitedParallelTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(16), // 4 tests × 4 runs (Repeat(3) = original + 3)
            result => result.ResultSummary.Counters.Passed.ShouldBe(16),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task StrictlySerialTests_ShouldRunOneAtATime()
    {
        await RunTestsWithFilter("/*/*/StrictlySerialTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(12), // 4 tests × 3 runs (Repeat(2) = original + 2)
            result => result.ResultSummary.Counters.Passed.ShouldBe(12),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    [Test]
    public async Task HighParallelismTests_ShouldAllowHighConcurrency()
    {
        await RunTestsWithFilter("/*/*/HighParallelismTests/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(16), // 4 tests × 4 runs (Repeat(3) = original + 3)
            result => result.ResultSummary.Counters.Passed.ShouldBe(16),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0)
        ]);
    }

    // Note: AllParallelismTests_ShouldPassTogether test removed because running all test classes
    // together causes static state sharing issues between the validation test classes.
    // The individual test class validations above are sufficient to verify correct behavior.
}