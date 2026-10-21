using TUnit.Core.Enums;
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
/// tests queued behind it, so the scheduling phases throw. The After(TestSession) hook must
/// still be awaited before the session disposes shared fixtures, rather than left running in
/// the background.
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class KeyedNotInParallelFailFast7074
{
    [ClassDataSource<FailFastSessionFixture7074>(Shared = SharedType.PerTestSession)]
    public required FailFastSessionFixture7074 Fixture { get; init; }

    // All tests share one key. The scheduler hands keys out in priority order while it walks
    // the tests, so Critical priority deterministically gives Fails the key first, and the
    // Waits tests queue behind it. (With distinct keys every test would start at once and race
    // for the single parallelism slot, so a Waits test could run before Fails.) When Fails
    // releases the key, fail-fast has already cancelled the token, so each Waits test throws
    // on its cancelled slot wait instead of running.
    [Test, NotInParallel("7074.ff"), ExecutionPriority(Priority.Critical)]
    public void Fails() => throw new InvalidOperationException("Intentional failure to trigger fail-fast");

    [Test, NotInParallel("7074.ff")] public Task Waits1() => Task.Delay(100);
    [Test, NotInParallel("7074.ff")] public Task Waits2() => Task.Delay(100);
    [Test, NotInParallel("7074.ff")] public Task Waits3() => Task.Delay(100);
    [Test, NotInParallel("7074.ff")] public Task Waits4() => Task.Delay(100);

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
