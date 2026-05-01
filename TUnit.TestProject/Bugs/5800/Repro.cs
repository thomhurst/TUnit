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
/// LonelyTest samples <c>_activeSiblings</c> throughout its body. With the
/// runtime <c>NotInParallelLock</c>, exclusive acquisition drains all readers
/// first AND blocks new readers until LonelyTest exits — so the count stays
/// provably zero. Without the fix, sibling activity overlaps and the count is
/// non-zero. No rendezvous is used: a rendezvous would deadlock under the fix
/// when LonelyTest's exclusive acquire wins the dispatch race against siblings'
/// shared acquire (siblings are then blocked from signalling).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Repro5800
{
    // Static fields are safe here: the engine test invokes Repro5800 in a
    // dedicated subprocess, so there is no cross-run state contamination.
    private static int _activeSiblings;
    private static int _maxSiblingsObservedDuringLonely;

    [Test, NotInParallel]
    public async Task LonelyTest()
    {
        // Sample over a window long enough that any concurrently-dispatched
        // sibling (held for SiblingHoldMs) would be observed if exclusion is
        // broken. Under the fix siblings are gated out and the count stays 0.
        for (var i = 0; i < 50; i++)
        {
            var snapshot = Volatile.Read(ref _activeSiblings);
            if (snapshot > _maxSiblingsObservedDuringLonely)
            {
                _maxSiblingsObservedDuringLonely = snapshot;
            }
            await Task.Delay(20);
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
    public async Task ParallelSibling(int _)
    {
        Interlocked.Increment(ref _activeSiblings);
        try
        {
            // Hold long enough for dep-recursion to surface LonelyTest while siblings
            // are still live (if exclusion is broken).
            await Task.Delay(1500);
        }
        finally
        {
            Interlocked.Decrement(ref _activeSiblings);
        }
    }
}
