using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6892;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/6892.
/// A registration receiver can remove the parallel constraints and the parallel limiter of a test.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel("Issue6892")]
[ParallelLimiter<Issue6892SerialLimit>]
[ClearParallelization]
public class ClearParallelizationTests
{
    private static readonly TaskCompletionSource FirstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static readonly TaskCompletionSource SecondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // See Repro5700 for why the rendezvous deadline is generous.
    private static readonly TimeSpan RendezvousTimeout = TimeSpan.FromSeconds(60);

    [Test]
    public async Task First()
    {
        await AssertNoParallelization();

        // Both tests share a NotInParallel key and a limit of 1. They finish only if they run at the same time.
        FirstStarted.TrySetResult();
        using var cts = new CancellationTokenSource(RendezvousTimeout);
        await SecondStarted.Task.WaitAsync(cts.Token);
    }

    [Test]
    public async Task Second()
    {
        await AssertNoParallelization();

        SecondStarted.TrySetResult();
        using var cts = new CancellationTokenSource(RendezvousTimeout);
        await FirstStarted.Task.WaitAsync(cts.Token);
    }

    private static async Task AssertNoParallelization()
    {
        var parallelism = TestContext.Current!.Parallelism;
        await Assert.That(parallelism.Constraints).IsEmpty();
        await Assert.That(parallelism.Limiter).IsNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ClearParallelLimiterThenSetTests
{
    [Test]
    [ParallelLimiter<Issue6892SerialLimit>]
    [ClearParallelization]
    [SetIssue6892WideLimiter]
    public async Task LimiterSetAfterClearIsApplied()
    {
        await Assert.That(TestContext.Current!.Parallelism.Limiter).IsTypeOf<Issue6892WideLimit>();
    }
}

internal sealed class ClearParallelizationAttribute : Attribute, ITestRegisteredEventReceiver
{
    // Runs after ParallelLimiterAttribute (Order 0).
    public int Order => 100;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.ClearParallelConstraints();
        context.ClearParallelLimiter();
        return default;
    }
}

internal sealed class SetIssue6892WideLimiterAttribute : Attribute, ITestRegisteredEventReceiver
{
    public int Order => 200;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetParallelLimiter(new Issue6892WideLimit());
        return default;
    }
}

public sealed class Issue6892SerialLimit : IParallelLimit
{
    public int Limit => 1;
}

public sealed class Issue6892WideLimit : IParallelLimit
{
    public int Limit => 8;
}
