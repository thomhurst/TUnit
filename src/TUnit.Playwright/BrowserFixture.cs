using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// The injected <see cref="PlaywrightFixture"/> is hardcoded to <see cref="SharedType.PerTestSession"/>.
/// Authoring a new fixture class is the way to change that scope — attribute arguments on
/// inherited <c>init</c> properties cannot be overridden.
/// </summary>
public class BrowserFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly SemaphoreSlim _relaunch = new(1, 1);

    [ClassDataSource<PlaywrightFixture>(Shared = SharedType.PerTestSession)]
    public required PlaywrightFixture PlaywrightFixture { get; init; }

    public IBrowser Browser { get; internal set; } = null!;

    /// <summary>
    /// When the shared <see cref="Browser"/> disconnected without TUnit closing it (for example
    /// because it crashed), the time it disconnected, in UTC; otherwise <c>null</c>.
    /// </summary>
    public DateTimeOffset? DisconnectedAt => Browser is { } browser ? PlaywrightBrowserMonitor.For(browser).DisconnectedAt : null;

    public virtual string BrowserName => Microsoft.Playwright.BrowserType.Chromium;

    protected virtual BrowserTypeLaunchOptions GetLaunchOptions() =>
        TUnitPlaywrightSettings.Default.DefaultBrowserTypeLaunchOptions ?? new BrowserTypeLaunchOptions();

    public virtual async Task InitializeAsync()
    {
        var browserType = PlaywrightFixture.Playwright[BrowserName]
            ?? throw new InvalidOperationException($"Unknown BrowserName '{BrowserName}'.");

        Browser = await PlaywrightServiceConnector.LaunchAsync(browserType, GetLaunchOptions()).ConfigureAwait(false);
        // Watch from launch, so a crash before the first context is created is still reported.
        PlaywrightBrowserMonitor.For(Browser);
    }

    /// <summary>
    /// Returns the browser for a new context. When the browser disconnected and
    /// <see cref="TUnitPlaywrightSettings.RelaunchDisconnectedBrowser"/> is set, launches it again first.
    /// </summary>
    internal async Task<IBrowser> GetBrowserForNewContextAsync()
    {
        var browser = Browser;
        if (!TUnitPlaywrightSettings.Default.RelaunchDisconnectedBrowser || browser is null ||
            PlaywrightBrowserMonitor.For(browser).DisconnectedAt is null)
        {
            return browser!;
        }

        await _relaunch.WaitAsync().ConfigureAwait(false);
        try
        {
            // Another test may have relaunched the browser while this one waited.
            if (ReferenceEquals(Browser, browser))
            {
                await InitializeAsync().ConfigureAwait(false);
            }

            return Browser;
        }
        finally
        {
            _relaunch.Release();
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            PlaywrightBrowserMonitor.MarkClosing(Browser);
            await Browser.CloseAsync().ConfigureAwait(false);
        }
    }
}
