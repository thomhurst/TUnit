using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7074;

/// <summary>
/// Regression for https://github.com/thomhurst/TUnit/issues/7074.
/// [NotInParallel] tests with distinct keys must still respect --maximum-parallel-tests.
/// The cap is only asserted when the engine test sets <see cref="ExpectedMaxVariable"/>,
/// so this class passes under any parallelism setting when run directly.
/// </summary>
internal sealed class ConcurrencyProbe7074
{
    public const string ExpectedMaxVariable = "TUNIT_7074_EXPECTED_MAX_CONCURRENCY";

    private int _current;
    private int _max;

    public async Task RunAsync()
    {
        var now = Interlocked.Increment(ref _current);
        int seen;
        while (now > (seen = Volatile.Read(ref _max)))
        {
            Interlocked.CompareExchange(ref _max, now, seen);
        }

        try
        {
            await Task.Delay(200);
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }

        if (int.TryParse(Environment.GetEnvironmentVariable(ExpectedMaxVariable), out var expectedMax)
            && Volatile.Read(ref _max) > expectedMax)
        {
            throw new InvalidOperationException(
                $"Observed {Volatile.Read(ref _max)} concurrent keyed tests, expected at most {expectedMax}");
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class KeyedNotInParallelMaxParallelism7074
{
    private static readonly ConcurrencyProbe7074 Probe = new();

    [Test, NotInParallel("7074.k1")] public Task T1() => Probe.RunAsync();
    [Test, NotInParallel("7074.k2")] public Task T2() => Probe.RunAsync();
    [Test, NotInParallel("7074.k3")] public Task T3() => Probe.RunAsync();
    [Test, NotInParallel("7074.k4")] public Task T4() => Probe.RunAsync();
    [Test, NotInParallel("7074.k5")] public Task T5() => Probe.RunAsync();
    [Test, NotInParallel("7074.k6")] public Task T6() => Probe.RunAsync();
    [Test, NotInParallel("7074.k7")] public Task T7() => Probe.RunAsync();
    [Test, NotInParallel("7074.k8")] public Task T8() => Probe.RunAsync();
}

/// <summary>
/// Same as <see cref="KeyedNotInParallelMaxParallelism7074"/>, but the keyed tests sit inside a
/// [ParallelGroup], which routes them through the constrained-parallel-group keyed path.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ParallelGroup("7074.Group")]
public class GroupedKeyedNotInParallelMaxParallelism7074
{
    private static readonly ConcurrencyProbe7074 Probe = new();

    [Test, NotInParallel("7074.g1")] public Task T1() => Probe.RunAsync();
    [Test, NotInParallel("7074.g2")] public Task T2() => Probe.RunAsync();
    [Test, NotInParallel("7074.g3")] public Task T3() => Probe.RunAsync();
    [Test, NotInParallel("7074.g4")] public Task T4() => Probe.RunAsync();
    [Test, NotInParallel("7074.g5")] public Task T5() => Probe.RunAsync();
    [Test, NotInParallel("7074.g6")] public Task T6() => Probe.RunAsync();
    [Test, NotInParallel("7074.g7")] public Task T7() => Probe.RunAsync();
    [Test, NotInParallel("7074.g8")] public Task T8() => Probe.RunAsync();
}

/// <summary>
/// Unconstrained and keyed tests run in concurrent scheduling phases, so they must draw
/// from one shared --maximum-parallel-tests budget rather than one budget each.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class MixedKeyedAndUnconstrainedMaxParallelism7074
{
    private static readonly ConcurrencyProbe7074 Probe = new();

    [Test] public Task U1() => Probe.RunAsync();
    [Test] public Task U2() => Probe.RunAsync();
    [Test] public Task U3() => Probe.RunAsync();
    [Test] public Task U4() => Probe.RunAsync();
    [Test, NotInParallel("7074.m1")] public Task K1() => Probe.RunAsync();
    [Test, NotInParallel("7074.m2")] public Task K2() => Probe.RunAsync();
    [Test, NotInParallel("7074.m3")] public Task K3() => Probe.RunAsync();
    [Test, NotInParallel("7074.m4")] public Task K4() => Probe.RunAsync();
}
