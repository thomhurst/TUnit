using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5700;

/// <summary>
/// Companion to Repro5700. Verifies the symmetric scenario for #5700:
/// two [NotInParallel] tests with different keys live in the same keyed
/// bucket and must overlap each other (their keys do not intersect).
/// Uses a TaskCompletionSource rendezvous so it does not depend on timing.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class CrossKeyOverlap5700
{
    private const string KeyA = "Tests.5700.A";
    private const string KeyB = "Tests.5700.B";

    private static readonly TaskCompletionSource KeyALive = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static readonly TaskCompletionSource KeyBLive = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [Test]
    [NotInParallel(KeyA)]
    public async Task KeyedA_RunsAlongsideKeyedB()
    {
        KeyALive.TrySetResult();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await KeyBLive.Task.WaitAsync(cts.Token);
    }

    [Test]
    [NotInParallel(KeyB)]
    public async Task KeyedB_RunsAlongsideKeyedA()
    {
        KeyBLive.TrySetResult();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await KeyALive.Task.WaitAsync(cts.Token);
    }
}
