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

    private readonly List<IBrowserContext> _contexts = [];
    private readonly object _contextsLock = new();
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
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
}
