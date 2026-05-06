using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

public class BrowserFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<PlaywrightFixture>(Shared = SharedType.PerTestSession)]
    public required PlaywrightFixture PlaywrightFixture { get; init; }

    public IBrowser Browser { get; private set; } = null!;

    public virtual string BrowserName => Microsoft.Playwright.BrowserType.Chromium;

    protected virtual BrowserTypeLaunchOptions GetLaunchOptions() => new();

    public virtual async Task InitializeAsync()
    {
        var browserType = PlaywrightFixture.Playwright[BrowserName]
            ?? throw new InvalidOperationException($"Unknown BrowserName '{BrowserName}'.");

        Browser = await PlaywrightServiceConnector.LaunchAsync(browserType, GetLaunchOptions()).ConfigureAwait(false);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync().ConfigureAwait(false);
        }
    }
}
