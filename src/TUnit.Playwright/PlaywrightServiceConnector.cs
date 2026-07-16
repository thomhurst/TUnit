using System.Globalization;
using Microsoft.Playwright;

namespace TUnit.Playwright;

internal static class PlaywrightServiceConnector
{
    private const string ApiVersion = "2023-10-01-preview";
    private const int ConnectTimeoutMs = 3 * 60 * 1000;

    public static async Task<IBrowser> LaunchAsync(IBrowserType browserType, BrowserTypeLaunchOptions options)
    {
        var accessToken = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_ACCESS_TOKEN");
        var serviceUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL");

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(serviceUrl))
        {
            return await browserType.LaunchAsync(options).ConfigureAwait(false);
        }

        var exposeNetwork = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_EXPOSE_NETWORK") ?? "<loopback>";
        var os = Uri.EscapeDataString(Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_OS") ?? "linux");
        var runId = Uri.EscapeDataString(Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_RUN_ID")
            ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        var wsEndpoint = $"{serviceUrl}?os={os}&runId={runId}&api-version={ApiVersion}";

        var connectOptions = new BrowserTypeConnectOptions
        {
            Timeout = ConnectTimeoutMs,
            ExposeNetwork = exposeNetwork,
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {accessToken}" }
        };

        // BrowserTypeLaunchOptions are local-process only; remote connect uses BrowserTypeConnectOptions.
        return await browserType.ConnectAsync(wsEndpoint, connectOptions).ConfigureAwait(false);
    }
}
