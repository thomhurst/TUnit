using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5800;

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/5800.
///
/// <c>[NotInParallel]</c> with no keys must run completely alone — no other test
/// executes at the same time. Before the fix, phase ordering alone enforced this:
/// the global NotInParallel bucket drained last in <c>TestScheduler</c>. But a
/// Parallel-bucket test with <c>[DependsOn(LonelyTest)]</c> triggered LonelyTest
/// mid–parallel-phase via dependency recursion in <c>TestRunner</c>, where it
/// then ran alongside its parallel siblings.
///
/// The rendezvous keeps the test deterministic: LonelyTest only starts checking
/// after at least one ParallelSibling has acquired its shared slot. With the
/// runtime <c>NotInParallelLock</c>, LonelyTest's exclusive acquisition drains
/// all readers first — so <c>_activeSiblings</c> is provably zero throughout
/// LonelyTest's body. Without the fix, sibling activity overlaps LonelyTest and
/// the assertion fails.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Repro5800
{
    private static int _activeSiblings;
    private static int _maxSiblingsObservedDuringLonely;
    private static readonly TaskCompletionSource SiblingLive = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Generous because Repro5800 runs inside the engine-test subprocess populated
    // with every [EngineTest=Pass] test in the suite. Sibling dispatch can be
    // delayed by the saturated parallel queue. A real bug — global NIP exclusion
    // not enforced — surfaces as a non-zero observed sibling count, not a timeout.
    private static readonly TimeSpan RendezvousTimeout = TimeSpan.FromSeconds(60);

    [Test, NotInParallel]
    public async Task LonelyTest()
    {
        using var cts = new CancellationTokenSource(RendezvousTimeout);
        await SiblingLive.Task.WaitAsync(cts.Token);

        for (var i = 0; i < 10; i++)
        {
            var snapshot = Volatile.Read(ref _activeSiblings);
            if (snapshot > _maxSiblingsObservedDuringLonely)
            {
                _maxSiblingsObservedDuringLonely = snapshot;
            }
            await Task.Delay(10);
        }

        await Assert.That(_maxSiblingsObservedDuringLonely)
            .IsZero()
            .Because("global [NotInParallel] must execute alone — even when triggered by a dependent's DependsOn recursion");
    }

    [Test, DependsOn(nameof(LonelyTest))]
    public async Task DependentOfLonely()
    {
        await Task.CompletedTask;
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    public async Task ParallelSibling(int n)
    {
        Interlocked.Increment(ref _activeSiblings);
        try
        {
            SiblingLive.TrySetResult();
            // Hold long enough for dep-recursion to surface LonelyTest while siblings
            // are still live (if exclusion is broken).
            await Task.Delay(800);
        }
        finally
        {
            Interlocked.Decrement(ref _activeSiblings);
        }
    }
}
