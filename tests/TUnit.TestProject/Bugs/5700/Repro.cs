using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5700;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/discussions/5700.
///
/// Test1 has no parallel constraint. Test2 has [NotInParallel(Test2Key)] — a
/// keyed constraint that should only block other tests sharing the key (per
/// docs/execution/parallelism.md). The buggy scheduler ran all unconstrained
/// tests to completion before starting keyed-NotInParallel tests, so Test1
/// never overlapped Test2.
///
/// The test uses a rendezvous (TaskCompletionSource) rather than wall-clock
/// sampling so it stays deterministic on slow CI runners: Test1 waits for
/// any Test2 invocation to start, and each Test2 invocation waits for Test1
/// to be live. Either side timing out → fix is broken.
///
/// Test2 also asserts the keyed constraint still serializes its own invocations.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Repro5700
{
    private const string Test2Key = "Tests.Test2";

    private static readonly TaskCompletionSource Test1Live = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static readonly TaskCompletionSource Test2Live = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static int _test2Active;
    private static int _test2ConcurrentViolations;

    // Deadline is generous because the engine-test harness runs Repro5700 inside a
    // subprocess populated with every `[EngineTest=Pass]` test in the project
    // (hundreds). On a busy CI runner the parallel queue can be saturated by other
    // unconstrained tests, so allow a full minute for either side to be dispatched.
    // The bug being guarded — keyed tests deferred behind the entire parallel
    // bucket — would manifest as Test1Live/Test2Live never being set at all, not as
    // a 60-second scheduling delay.
    private static readonly TimeSpan RendezvousTimeout = TimeSpan.FromSeconds(60);

    [Test]
    public async Task Test1_RunsAlongsideKeyedTest2()
    {
        Test1Live.TrySetResult();

        using var cts = new CancellationTokenSource(RendezvousTimeout);
        await Test2Live.Task.WaitAsync(cts.Token);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [NotInParallel(Test2Key)]
    public async Task Test2_KeyedSerializes(int param)
    {
        var concurrent = Interlocked.Increment(ref _test2Active);
        try
        {
            if (concurrent > 1)
            {
                Interlocked.Increment(ref _test2ConcurrentViolations);
            }

            Test2Live.TrySetResult();

            using var cts = new CancellationTokenSource(RendezvousTimeout);
            await Test1Live.Task.WaitAsync(cts.Token);

            // Hold the key briefly so the second Test2 invocation queues behind
            // us, exposing any concurrency-violation in keyed serialization.
            await Task.Delay(200);
        }
        finally
        {
            Interlocked.Decrement(ref _test2Active);
        }
    }

    [After(Class)]
    public static async Task AssertKeyedSerialization()
    {
        await Assert.That(_test2ConcurrentViolations).IsZero();
    }
}
