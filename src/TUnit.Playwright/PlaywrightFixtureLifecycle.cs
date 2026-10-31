using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Playwright;

internal sealed class PlaywrightFixtureLifecycle
{
    private readonly Lock _lock = new();
    private TestContext? _owner;
    private bool _recording;
    private int _attempt;
    private Task? _initialization;

    internal bool IsInitialized => _initialization?.Status == TaskStatus.RanToCompletion;

    /// <summary>
    /// The test whose failure capture applies to the current initialization, or <c>null</c>.
    /// Set before the fixture initializes.
    /// </summary>
    internal TestContext? CaptureOwner { get; private set; }

    /// <summary>Stops a later direct <c>InitializeAsync</c> call from reusing a stale owner.</summary>
    internal void ClearCaptureOwner() => CaptureOwner = null;

    internal Task InitializeAsync(IAsyncInitializer fixture, TestContext context)
    {
        lock (_lock)
        {
            var recording = PlaywrightContextOptions.Recording(context) is not null;
            var sharedWithOtherTests = (_owner is not null && !ReferenceEquals(_owner, context)) || ObjectTracker.IsShared(fixture);
            if ((recording || _recording) && sharedWithOtherTests)
            {
                throw new InvalidOperationException(
                    "[RecordVideo] requires ContextFixture and PageFixture instances private to one test. " +
                    "Use SharedType.None for these fixtures; BrowserFixture may be shared.");
            }

#if NET
            var declaredShared = TraceScopeRegistry.GetSharedType(fixture) is { } sharedType && sharedType != SharedType.None;
            if (recording && declaredShared)
            {
                throw new InvalidOperationException(
                    "[RecordVideo] requires SharedType.None for ContextFixture and PageFixture. BrowserFixture may be shared.");
            }
#else
            const bool declaredShared = false;
#endif

            // A shared fixture has no single owning test, so failure capture skips it instead of failing.
            var capture = !sharedWithOtherTests && !declaredShared && PlaywrightFailureCapture.Options(context) is not null;
            var perAttempt = recording || capture;

            if (_initialization is not null && (!perAttempt || _attempt == context.Execution.CurrentRetryAttempt))
            {
                return _initialization;
            }

            _owner ??= context;
            _recording = recording;
            _attempt = context.Execution.CurrentRetryAttempt;
            CaptureOwner = capture ? context : null;
            if (perAttempt)
            {
                // Register before initialization so partial setup still gets cleanup.
                PlaywrightRecordingScope.For(context).Register(() => CompleteAsync((IAsyncDisposable)fixture), fixture is PageFixture ? 0 : 1);
            }

            // Cache failures within an attempt, just like ordinary fixture initialization.
            _initialization = InvokeAsync(fixture);
            return _initialization;
        }
    }

    private static async Task InvokeAsync(IAsyncInitializer fixture) => await fixture.InitializeAsync().ConfigureAwait(false);

    private async ValueTask CompleteAsync(IAsyncDisposable fixture)
    {
        try
        {
            // Cancellation can stop the caller waiting while asynchronous setup continues.
            // Let setup settle before disposing any resources it managed to create.
            if (_initialization is { } initialization)
            {
                await initialization.ConfigureAwait(false);
            }
        }
        catch
        {
            // The engine already observes initialization failure. Cleanup must still run.
        }

        await fixture.DisposeAsync().ConfigureAwait(false);
    }
}
