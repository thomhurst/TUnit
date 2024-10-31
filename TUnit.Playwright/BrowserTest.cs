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
    private readonly BrowserTypeLaunchOptions _options;

    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        var context = await Browser.NewContextAsync(options).ConfigureAwait(false);
        _contexts.Add(context);
        return context;
    }

    [Before(HookType.Test)]
    public async Task BrowserSetup()
    {
        var service = await BrowserService.Register(this, BrowserType, _options).ConfigureAwait(false);
        Browser = service.Browser;
    }

    [After(HookType.Test)]
    public async Task BrowserTearDown(TestContext testContext)
    {
        if (TestOk(testContext))
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
