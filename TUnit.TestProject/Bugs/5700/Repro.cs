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
/// Assertions:
/// - Test1 must observe Test2 running concurrently (proves cross-phase parallelism).
/// - Test2 invocations must never run concurrently with each other (proves keyed
///   serialization still works).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Repro5700
{
    private const string Test2Key = "Tests.Test2";

    private static int _test2Active;
    private static int _test1ObservedTest2;
    private static int _test2ConcurrentViolations;

    [Test]
    public async Task Test1_RunsAlongsideKeyedTest2()
    {
        // Test1 = 3s sample window; Test2 cases serialize to ~2s, so Test1
        // must overlap at least one Test2 invocation when the fix is in place.
        for (var i = 0; i < 60; i++)
        {
            if (Volatile.Read(ref _test2Active) > 0)
            {
                Interlocked.Increment(ref _test1ObservedTest2);
            }
            await Task.Delay(50);
        }
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
            await Task.Delay(1000);
        }
        finally
        {
            Interlocked.Decrement(ref _test2Active);
        }
    }

    [After(Class)]
    public static async Task AssertParallelismShape()
    {
        await Assert.That(_test2ConcurrentViolations).IsZero();
        await Assert.That(_test1ObservedTest2).IsGreaterThan(0);
    }
}
