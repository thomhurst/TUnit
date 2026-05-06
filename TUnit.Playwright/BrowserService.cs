using Microsoft.Playwright;

namespace TUnit.Playwright;

internal class BrowserService : IWorkerService
{
    public IBrowser Browser { get; private set; }

    private BrowserService(IBrowser browser)
    {
        Browser = browser;
    }

    public static Task<BrowserService> Register(
        WorkerAwareTest test,
        IBrowserType browserType,
        BrowserTypeLaunchOptions options)
    {
        return test.RegisterService("Browser", async () =>
            new BrowserService(await PlaywrightServiceConnector.LaunchAsync(browserType, options).ConfigureAwait(false)));
    }

    public Task ResetAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        var browser = Browser;

        if (browser != null)
        {
            await browser.CloseAsync().ConfigureAwait(false);
        }
    }
}
