using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7074;

public sealed class FailFastSessionFixture7074 : IAsyncDisposable
{
    public static int Disposed;

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref Disposed, 1);
        return default;
    }
}

/// <summary>
/// With --fail-fast and --maximum-parallel-tests 1, the failing keyed test cancels the keyed
/// tests still waiting for a parallelism slot. The After(TestSession) hook must still be awaited
/// before the session disposes shared fixtures, rather than left running in the background.
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class KeyedNotInParallelFailFast7074
{
    [ClassDataSource<FailFastSessionFixture7074>(Shared = SharedType.PerTestSession)]
    public required FailFastSessionFixture7074 Fixture { get; init; }

    [Test, NotInParallel("7074.ff1")]
    public void Fails() => throw new InvalidOperationException("Intentional failure to trigger fail-fast");

    [Test, NotInParallel("7074.ff2")] public Task Waits1() => Task.Delay(100);
    [Test, NotInParallel("7074.ff3")] public Task Waits2() => Task.Delay(100);
    [Test, NotInParallel("7074.ff4")] public Task Waits3() => Task.Delay(100);
    [Test, NotInParallel("7074.ff5")] public Task Waits4() => Task.Delay(100);

    // No-op unless the engine test opted in via the environment variable, so this hook is
    // inert for every other TestProject invocation.
    [After(TestSession)]
    public static async Task WriteSessionHookMarker()
    {
        var markerPath = Environment.GetEnvironmentVariable("TUNIT_7074_SESSION_HOOK_MARKER_PATH");
        if (string.IsNullOrEmpty(markerPath))
        {
            return;
        }

        // Yield long enough that a session which does not await this hook would already
        // have disposed the shared fixture (or exited) by the time the marker is written.
        await Task.Delay(1000);

        File.WriteAllText(markerPath, $"FixtureDisposedDuringHook={Volatile.Read(ref FailFastSessionFixture7074.Disposed) == 1}");
    }
}
