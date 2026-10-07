using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class BrowserTest : PlaywrightTest
{
    public BrowserTest() : this(TUnitPlaywrightSettings.Default.DefaultBrowserTypeLaunchOptions ?? new BrowserTypeLaunchOptions())
    {
    }

    public BrowserTest(BrowserTypeLaunchOptions options)
    {
        _options = options;
    }

    public IBrowser Browser { get; internal set; } = null!;

    /// <summary>
    /// Adds W3C <c>traceparent</c>/<c>baggage</c> headers from the current test's
    /// <see cref="System.Diagnostics.Activity"/> to requests that contexts created via
    /// <see cref="NewContext"/> send to <see cref="TraceContextOrigins"/>. Override to
    /// <c>false</c> to disable propagation entirely.
    /// </summary>
    /// <remarks>
    /// Has no effect on <c>netstandard2.0</c> targets — the engine's Activity plumbing
    /// is .NET-only.
    /// </remarks>
    public virtual bool PropagateTraceContext => true;

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
    public virtual IReadOnlyList<string>? TraceContextOrigins => null;

    private readonly List<Task<(IBrowserContext Context, PlaywrightVideoRecorder? Recording)>> _contexts = [];
    private readonly Lock _contextsLock = new();
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        var owner = TestContext.Current;
        var traceContext = PlaywrightTraceContextRoute.Create(PropagateTraceContext, TraceContextOrigins, options);
        Task<(IBrowserContext Context, PlaywrightVideoRecorder? Recording)> creation;
        lock (_contextsLock)
        {
            var browser = Browser ?? throw new InvalidOperationException("Cannot create a browser context before setup or after teardown has started.");
            creation = CreateContextAsync(browser, options, traceContext, owner);
            // Track pending creation too, so teardown waits for every context it owns.
            _contexts.Add(creation);
        }

        return (await creation.ConfigureAwait(false)).Context;
    }

    private static async Task<(IBrowserContext Context, PlaywrightVideoRecorder? Recording)> CreateContextAsync(
        IBrowser browser, BrowserNewContextOptions options, PlaywrightTraceContextRoute? traceContext, TestContext? owner)
    {
        var context = await browser.NewContextAsync(options).ConfigureAwait(false);
        if (traceContext is not null)
        {
            try
            {
                await traceContext.RegisterAsync(context).ConfigureAwait(false);
            }
            catch
            {
                await context.CloseAsync().ConfigureAwait(false);
                throw;
            }
        }

        var recording = !string.IsNullOrEmpty(options.RecordVideoDir) && owner is not null
            ? new PlaywrightVideoRecorder(context, owner)
            : null;

        return (context, recording);
    }

    [Before(HookType.Test, "", 0)]
    public async Task BrowserSetup()
    {
        if (BrowserType == null)
        {
            throw new InvalidOperationException($"BrowserType is not initialized. This may indicate that {nameof(PlaywrightTest)}.{nameof(Playwright)} is not initialized or {nameof(PlaywrightTest)}.{nameof(PlaywrightSetup)} did not execute properly.");
        }

        var service = await BrowserService.Register(this, BrowserType, _options).ConfigureAwait(false);
        lock (_contextsLock)
        {
            Browser = service.Browser;
        }
    }

    [After(HookType.Test, "", 0)]
    public async Task BrowserTearDown(TestContext testContext)
    {
        Task<(IBrowserContext Context, PlaywrightVideoRecorder? Recording)>[] contexts;
        lock (_contextsLock)
        {
            Browser = null!;
            contexts = _contexts.ToArray();
            _contexts.Clear();
        }

        List<Exception>? exceptions = null;
        foreach (var creation in contexts)
        {
            (IBrowserContext Context, PlaywrightVideoRecorder? Recording) created;
            try
            {
                created = await creation.ConfigureAwait(false);
            }
            catch
            {
                // NewContext reports creation failures; there is no context to close.
                continue;
            }

            try
            {
                var (context, recording) = created;
                await (recording?.CloseAsync() ?? context.CloseAsync()).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
        }

        if (exceptions is not null)
        {
            throw new AggregateException("One or more browser contexts failed to close.", exceptions);
        }
    }
}
