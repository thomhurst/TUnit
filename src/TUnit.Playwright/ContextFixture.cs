using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// The injected <see cref="BrowserFixture"/> is hardcoded to <see cref="SharedType.PerTestSession"/>.
/// Authoring a new fixture class is the way to change that scope — attribute arguments on
/// inherited <c>init</c> properties cannot be overridden.
/// </summary>
public class ContextFixture : IAsyncInitializer, IAsyncDisposable, ITestAttemptInitializer, ITestEndEventReceiver
{
    private readonly PlaywrightFixtureLifecycle _lifecycle = new();
    private IBrowserContext? _context;
    private PlaywrightVideoRecorder? _recording;
    private PlaywrightFailureCapture? _capture;

    /// <summary>The monitor of the browser that created the current context.</summary>
    internal PlaywrightBrowserMonitor? BrowserMonitor { get; private set; }

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
    /// that context; set <see cref="PropagateTraceContext"/> to <c>false</c> to keep the cache.
    /// Routes you register later run first; call <c>route.FallbackAsync()</c> rather than
    /// <c>route.ContinueAsync()</c> in them to keep the headers. Context routes do not see
    /// WebSocket traffic or requests handled by a service worker, so those requests get no
    /// headers. Playwright keeps the headers when a routed request is redirected, including
    /// redirects to another origin.
    /// </remarks>
    protected virtual IReadOnlyList<string>? TraceContextOrigins => null;

    public virtual async Task InitializeAsync()
    {
        var owner = TestContext.Current;
        var captureOwner = _lifecycle.CaptureOwner;
        _lifecycle.ClearCaptureOwner();
        _recording = null;
        _capture = null;
        var options = PlaywrightContextOptions.ApplyRecording(GetContextOptions(), owner);
        var traceContext = PlaywrightTraceContextRoute.Create(PropagateTraceContext, TraceContextOrigins, options);
        var browser = await BrowserFixture.GetBrowserForNewContextAsync().ConfigureAwait(false);
        var monitor = BrowserMonitor = PlaywrightBrowserMonitor.RecordUse(owner, browser);
        try
        {
            _context = await browser.NewContextAsync(options).ConfigureAwait(false);
        }
        catch (Exception exception) when (monitor.WrapIfDisconnected(exception, owner) is { } wrapped)
        {
            throw wrapped;
        }

        try
        {
            if (traceContext is not null)
            {
                await traceContext.RegisterAsync(_context).ConfigureAwait(false);
            }

            _capture = await PlaywrightFailureCapture.StartAsync(_context, captureOwner).ConfigureAwait(false);
        }
        catch
        {
            if (Interlocked.Exchange(ref _context, null) is { } context)
            {
                await context.CloseAsync().ConfigureAwait(false);
            }

            throw;
        }

        // Option-only recording can span shared fixture lifetimes and has no single test owner.
        _recording = PlaywrightContextOptions.Recording(owner) is not null && owner is not null
            ? new PlaywrightVideoRecorder(_context, owner)
            : null;
    }

    bool ITestAttemptInitializer.IsInitialized => _lifecycle.IsInitialized;

    async ValueTask ITestAttemptInitializer.InitializeForTestAttemptAsync(TestContext context, CancellationToken cancellationToken) =>
        await _lifecycle.InitializeAsync(this, context).WaitAsync(cancellationToken).ConfigureAwait(false);

    int IEventReceiver.Order => 0;

    ValueTask ITestEndEventReceiver.OnTestEnd(TestContext context)
    {
        if (BrowserMonitor is { } monitor)
        {
            PlaywrightBrowserMonitor.RecordUse(context, monitor);
        }

        PlaywrightBrowserMonitor.ReportIfDisconnected(context);
        return default;
    }

    public virtual async ValueTask DisposeAsync()
    {
        // Save failure artifacts before the context closes; this is a no-op when already saved.
        if (Interlocked.Exchange(ref _capture, null) is { } capture)
        {
            await capture.CompleteAsync().ConfigureAwait(false);
        }

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
