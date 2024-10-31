using System.Globalization;
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
        return test.RegisterService("Browser", async () => new BrowserService(await CreateBrowser(browserType, options).ConfigureAwait(false)));
    }

    private static async Task<IBrowser> CreateBrowser(
        IBrowserType browserType,
        BrowserTypeLaunchOptions options)
    {
        var accessToken = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_ACCESS_TOKEN");
        var serviceUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL");

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(serviceUrl))
        {
            return await browserType.LaunchAsync(options).ConfigureAwait(false);
        }

        var exposeNetwork = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_EXPOSE_NETWORK") ?? "<loopback>";
        var os = Uri.EscapeDataString(Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_OS") ?? "linux");
        var runId = Uri.EscapeDataString(Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_RUN_ID") ?? DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        var apiVersion = "2023-10-01-preview";
        var wsEndpoint = $"{serviceUrl}?os={os}&runId={runId}&api-version={apiVersion}";
        var connectOptions = new BrowserTypeConnectOptions
        {
            Timeout = 3 * 60 * 1000,
            ExposeNetwork = exposeNetwork,
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {accessToken}"
            }
        };

        return await browserType.ConnectAsync(wsEndpoint, connectOptions).ConfigureAwait(false);
    }

    public Task ResetAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Browser.CloseAsync();
}
