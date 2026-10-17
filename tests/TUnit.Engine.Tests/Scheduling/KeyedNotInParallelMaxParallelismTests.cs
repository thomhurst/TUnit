using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests.Scheduling;

/// <summary>
/// Regression for https://github.com/thomhurst/TUnit/issues/7074.
/// [NotInParallel] tests with distinct keys must not exceed --maximum-parallel-tests.
/// Unconstrained and keyed tests run concurrently, so they share the cap rather than getting one each.
/// The test project asserts the observed peak concurrency against the expected cap.
/// </summary>
public class KeyedNotInParallelMaxParallelismTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string ExpectedMaxVariable = "TUNIT_7074_EXPECTED_MAX_CONCURRENCY";

    [Test]
    [Arguments("KeyedNotInParallelMaxParallelism7074")]
    [Arguments("GroupedKeyedNotInParallelMaxParallelism7074")]
    [Arguments("MixedKeyedAndUnconstrainedMaxParallelism7074")]
    public async Task KeyedNotInParallel_DistinctKeys_RespectMaximumParallelTests(string className)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await RunTestsWithFilter($"/*/*/{className}/*",
        [
            result => result.ResultSummary.Outcome.ShouldBe("Completed"),
            result => result.ResultSummary.Counters.Total.ShouldBe(8),
            result => result.ResultSummary.Counters.Passed.ShouldBe(8),
            result => result.ResultSummary.Counters.Failed.ShouldBe(0),
        ],
        new RunOptions()
            .WithArgument("--maximum-parallel-tests")
            .WithArgument("2")
            .WithEnvironmentVariable(ExpectedMaxVariable, "2")
            .WithForcefulCancellationToken(cts.Token));
    }
}
