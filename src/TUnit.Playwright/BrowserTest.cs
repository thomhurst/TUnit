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
    /// Seeds each <see cref="IBrowserContext"/> created via <see cref="NewContext"/>
    /// with W3C <c>traceparent</c>/<c>baggage</c> headers from the current test's
    /// <see cref="System.Diagnostics.Activity"/>. Override to <c>false</c> to avoid
    /// leaking trace ids to third-party domains the page contacts.
    /// </summary>
    /// <remarks>
    /// Has no effect on <c>netstandard2.0</c> targets — the engine's Activity plumbing
    /// is .NET-only.
    /// </remarks>
    public virtual bool PropagateTraceContext => true;

    private readonly List<(IBrowserContext Context, PlaywrightVideoRecorder? Recording)> _contexts = [];
    private readonly Lock _contextsLock = new();
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        var owner = TestContext.Current;
        options = PlaywrightTelemetryHeaders.Merge(options, PropagateTraceContext);
        var context = await Browser.NewContextAsync(options).ConfigureAwait(false);
        var recording = !string.IsNullOrEmpty(options.RecordVideoDir) && owner is not null
            ? new PlaywrightVideoRecorder(context, owner)
            : null;

        lock (_contextsLock)
        {
            _contexts.Add((context, recording));
        }

        return context;
    }

    [Before(HookType.Test, "", 0)]
    public async Task BrowserSetup()
    {
        if (BrowserType == null)
        {
            throw new InvalidOperationException($"BrowserType is not initialized. This may indicate that {nameof(PlaywrightTest)}.{nameof(Playwright)} is not initialized or {nameof(PlaywrightTest)}.{nameof(PlaywrightSetup)} did not execute properly.");
        }

        var service = await BrowserService.Register(this, BrowserType, _options).ConfigureAwait(false);
        Browser = service.Browser;
    }

    [After(HookType.Test, "", 0)]
    public async Task BrowserTearDown(TestContext testContext)
    {
        (IBrowserContext Context, PlaywrightVideoRecorder? Recording)[] contexts;
        lock (_contextsLock)
        {
            contexts = _contexts.ToArray();
            _contexts.Clear();
        }

        List<Exception>? exceptions = null;
        foreach (var (context, recording) in contexts)
        {
            try
            {
                await (recording?.CloseAsync() ?? context.CloseAsync()).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
        }

        Browser = null!;
        if (exceptions is not null)
        {
            throw new AggregateException("One or more browser contexts failed to close.", exceptions);
        }
    }
}
