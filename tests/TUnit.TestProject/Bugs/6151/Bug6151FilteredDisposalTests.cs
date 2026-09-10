using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6151;

public sealed class SessionSharedFixture : IAsyncDisposable
{
    public static int CreatedCount;
    public static int DisposedCount;

    public SessionSharedFixture()
    {
        Interlocked.Increment(ref CreatedCount);
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref DisposedCount);
        return default;
    }
}

/// <summary>
/// Repro for https://github.com/thomhurst/TUnit/discussions/6151 — a PerTestSession shared
/// fixture must be disposed even when only a subset of the tests that consume it executes.
/// The [Explicit] sibling is built (incrementing the fixture's ref count) but excluded from
/// execution, mirroring how an IDE's uid filter selects a single [Arguments] case: the
/// never-executed test's ref count previously kept the fixture alive forever.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class Bug6151FilteredDisposalTests
{
    [ClassDataSource<SessionSharedFixture>(Shared = SharedType.PerTestSession)]
    public required SessionSharedFixture Fixture { get; init; }

    [Test]
    public void TestA()
    {
    }

    [Test]
    [Explicit]
    public void TestB()
    {
    }
}

public static class Bug6151SessionMarker
{
    // Shared-object disposal is ref-counted and fires when the last consuming test completes,
    // which is before After(TestSession) hooks run — so the counts are final here.
    // No-op unless the engine test opted in via the environment variable, so this hook is
    // inert for every other TestProject invocation.
    [After(TestSession)]
    public static void WriteDisposalMarker()
    {
        var markerPath = Environment.GetEnvironmentVariable("TUNIT_BUG_6151_MARKER_PATH");
        if (string.IsNullOrEmpty(markerPath))
        {
            return;
        }

        File.WriteAllText(markerPath, $"Created={SessionSharedFixture.CreatedCount};Disposed={SessionSharedFixture.DisposedCount}");

        // Reset so a later run session in the same process (IDE server mode) reports
        // per-session counts instead of cumulative ones.
        Interlocked.Exchange(ref SessionSharedFixture.CreatedCount, 0);
        Interlocked.Exchange(ref SessionSharedFixture.DisposedCount, 0);
    }
}
