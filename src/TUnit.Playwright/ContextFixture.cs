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
    /// When <c>true</c>, adds W3C trace propagation headers from the current test's
    /// <see cref="System.Diagnostics.Activity"/> to requests sent to
    /// <see cref="TraceContextOrigins"/>.
    /// </summary>
    protected virtual bool PropagateTraceContext => true;

    /// <summary>
    /// Origins (for example <c>https://localhost:5001</c>) whose requests receive trace
    /// context headers. Defaults to <c>null</c>, which uses the origin of
    /// <see cref="BrowserNewContextOptions.BaseURL"/>; when no base URL is set, no headers
    /// are sent. Requests to other origins never receive the headers, so third-party
    /// CORS requests are not preflighted and trace ids do not leak.
    /// </summary>
    /// <remarks>
    /// Headers are added by a context route, which disables Playwright's HTTP cache for
    /// that context. Routes you register later run first; call <c>route.FallbackAsync()</c>
    /// rather than <c>route.ContinueAsync()</c> in them to keep the headers.
    /// </remarks>
    protected virtual IReadOnlyList<string>? TraceContextOrigins => null;

    public virtual async Task InitializeAsync()
    {
        var owner = TestContext.Current;
        _recording = null;
        var options = PlaywrightContextOptions.ApplyRecording(GetContextOptions(), owner);
        var traceContext = PlaywrightTraceContextRoute.Create(PropagateTraceContext, TraceContextOrigins, options);
        _context = await BrowserFixture.Browser.NewContextAsync(options).ConfigureAwait(false);
        if (traceContext is not null)
        {
            await traceContext.RegisterAsync(_context).ConfigureAwait(false);
        }

        // Option-only recording can span shared fixture lifetimes and has no single test owner.
        _recording = PlaywrightContextOptions.Recording(owner) is not null && owner is not null
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
