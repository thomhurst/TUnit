namespace TUnit.Playwright;

/// <summary>
/// The order in which <see cref="PlaywrightRecordingScope"/> disposes registered resources at test
/// end, after every failure capture has run. Lower values run first.
/// </summary>
internal static class PlaywrightCleanupOrder
{
    /// <summary>Per-attempt <see cref="PageFixture"/> instances: pages close before their context.</summary>
    internal const int Page = 0;

    /// <summary>Per-attempt <see cref="ContextFixture"/> instances.</summary>
    internal const int Context = 1;

    /// <summary>Contexts that <see cref="BrowserTest.BrowserTearDown"/> left open for failure capture.</summary>
    internal const int DeferredContext = 2;

    /// <summary>Returns a <see cref="BrowserTest"/> worker to the pool once its deferred contexts are closed.</summary>
    internal const int WorkerRelease = 3;
}
