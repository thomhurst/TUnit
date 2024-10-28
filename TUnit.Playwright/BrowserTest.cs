using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class BrowserTest(BrowserTypeLaunchOptions options) : PlaywrightTest
{
    public BrowserTest() : this(new BrowserTypeLaunchOptions())
    {
    }
    
    public IBrowser Browser { get; internal set; } = null!;
    private readonly List<IBrowserContext> _contexts = new();

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions? options = null)
    {
        var context = await Browser.NewContextAsync(options).ConfigureAwait(false);
        _contexts.Add(context);
        return context;
    }

    [Before(HookType.Test)]
    public async Task BrowserSetup()
    {
        var service = await BrowserService.Register(this, Playwright, BrowserType, options).ConfigureAwait(false);
        Browser = service.Browser;
    }

    [After(HookType.Test)]
    public async Task BrowserTearDown()
    {
        if (TestOk())
        {
            foreach (var context in _contexts)
            {
                await context.CloseAsync().ConfigureAwait(false);
            }
        }
        _contexts.Clear();
        Browser = null!;
    }
}
