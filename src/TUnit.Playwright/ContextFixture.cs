using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// The injected <see cref="BrowserFixture"/> is hardcoded to <see cref="SharedType.PerTestSession"/>.
/// Authoring a new fixture class is the way to change that scope — attribute arguments on
/// inherited <c>init</c> properties cannot be overridden.
/// </summary>
public class ContextFixture : IAsyncInitializer, IAsyncDisposable, ITestAttemptInitializer
{
    private readonly PlaywrightFixtureLifecycle _lifecycle = new();
    private IBrowserContext? _context;
    private PlaywrightVideoRecorder? _recording;

    [ClassDataSource<BrowserFixture>(Shared = SharedType.PerTestSession)]
    public required BrowserFixture BrowserFixture { get; init; }

    public IBrowserContext Context => _context!;

    /// <summary>
    /// Returns the options used when creating each <see cref="IBrowserContext"/>. Defaults
    /// match <see cref="ContextTest.ContextOptions"/> — pinned <c>Locale = "en-US"</c> and
    /// <c>ColorScheme = Light</c> for deterministic cross-platform rendering. Override to
    /// match your application's locale or to restore browser-default behaviour
    /// (<c>new BrowserNewContextOptions()</c>).
    /// </summary>
    protected virtual BrowserNewContextOptions GetContextOptions() =>
        PlaywrightContextOptions.Defaults();

    /// <summary>
    /// When <c>true</c>, seeds the context with W3C trace propagation headers from
    /// the current test's <see cref="System.Diagnostics.Activity"/>.
    /// </summary>
    protected virtual bool PropagateTraceContext => true;

    public virtual async Task InitializeAsync()
    {
        var owner = TestContext.Current;
        _recording = null;
        var options = PlaywrightContextOptions.ApplyRecording(GetContextOptions(), owner);
        options = PlaywrightTelemetryHeaders.Merge(options, PropagateTraceContext);
        _context = await BrowserFixture.Browser.NewContextAsync(options).ConfigureAwait(false);
        _recording = !string.IsNullOrEmpty(options.RecordVideoDir) && owner is not null
            ? new PlaywrightVideoRecorder(_context, owner)
            : null;
    }

    bool ITestAttemptInitializer.IsInitialized => _lifecycle.IsInitialized;

    async ValueTask ITestAttemptInitializer.InitializeForTestAttemptAsync(TestContext context, CancellationToken cancellationToken) =>
        await _lifecycle.InitializeAsync(this, context).WaitAsync(cancellationToken).ConfigureAwait(false);

    public virtual async ValueTask DisposeAsync()
    {
        if (_recording is { } recording)
        {
            await recording.CloseAsync().ConfigureAwait(false);
            _context = null;
        }
        else if (Interlocked.Exchange(ref _context, null) is { } context)
        {
            await context.CloseAsync().ConfigureAwait(false);
        }
    }
}
