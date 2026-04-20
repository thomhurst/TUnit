using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class BrowserTest : PlaywrightTest
{
    public BrowserTest() : this(new BrowserTypeLaunchOptions())
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
    public virtual bool PropagateTraceContext => true;

    private readonly List<IBrowserContext> _contexts = [];
    private readonly Lock _contextsLock = new();
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        options = MergeTelemetryHeaders(options);
        var context = await Browser.NewContextAsync(options).ConfigureAwait(false);

        lock (_contextsLock)
        {
            _contexts.Add(context);
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
        List<IBrowserContext> contextsSnapshot;

        lock (_contextsLock)
        {
            contextsSnapshot = [.. _contexts];
            _contexts.Clear();
        }

        List<Exception>? exceptions = null;

        foreach (var context in contextsSnapshot)
        {
            try
            {
                await context.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        Browser = null!;

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("One or more browser contexts failed to close.", exceptions);
        }
    }

    private BrowserNewContextOptions MergeTelemetryHeaders(BrowserNewContextOptions options)
    {
#if NET
        if (!PropagateTraceContext || System.Diagnostics.Activity.Current is null)
        {
            return options;
        }

        // Seed user headers first so they win when the propagator tries to add the same key.
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (options.ExtraHTTPHeaders is not null)
        {
            foreach (var kvp in options.ExtraHTTPHeaders)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        var before = merged.Count;
        Telemetry.PlaywrightActivityPropagator.InjectInto(merged);
        if (merged.Count == before)
        {
            return options;
        }

        return new BrowserNewContextOptions(options) { ExtraHTTPHeaders = merged };
#else
        return options;
#endif
    }
}
