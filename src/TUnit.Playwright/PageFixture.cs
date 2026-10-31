using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// The injected <see cref="ContextFixture"/> defaults to <see cref="SharedType.None"/> (a fresh
/// context per <see cref="PageFixture"/>). Two <c>[ClassDataSource&lt;PageFixture&gt;]</c>
/// properties on the same test class therefore yield two isolated browser contexts while
/// sharing the underlying <see cref="BrowserFixture"/> at <see cref="SharedType.PerTestSession"/>.
/// </summary>
public class PageFixture : IAsyncInitializer, IAsyncDisposable, ITestAttemptInitializer, ITestEndEventReceiver
{
    private readonly PlaywrightFixtureLifecycle _lifecycle = new();
    private IPage? _page;

    [ClassDataSource<ContextFixture>]
    public required ContextFixture ContextFixture { get; init; }

    public IPage Page => _page!;

    public virtual async Task InitializeAsync()
    {
        // Preserve explicitly initialized contexts while allowing retries to recreate one.
        if (ContextFixture.Context is null)
        {
            await ObjectInitializer.InitializeAsync(ContextFixture).ConfigureAwait(false);
        }
        var context = ContextFixture.Context
            ?? throw new InvalidOperationException("ContextFixture did not create a browser context during initialization.");
        try
        {
            _page = await context.NewPageAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (ContextFixture.BrowserMonitor?.WrapIfDisconnected(exception, TestContext.Current) is { } wrapped)
        {
            throw wrapped;
        }
    }

    bool ITestAttemptInitializer.IsInitialized => _lifecycle.IsInitialized;

    async ValueTask ITestAttemptInitializer.InitializeForTestAttemptAsync(TestContext context, CancellationToken cancellationToken) =>
        await _lifecycle.InitializeAsync(this, context).WaitAsync(cancellationToken).ConfigureAwait(false);

    int IEventReceiver.Order => 0;

    ValueTask ITestEndEventReceiver.OnTestEnd(TestContext context)
    {
        if (ContextFixture.BrowserMonitor is { } monitor)
        {
            PlaywrightBrowserMonitor.RecordUse(context, monitor);
        }

        PlaywrightBrowserMonitor.ReportIfDisconnected(context);
        return default;
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _page, null) is { } page)
        {
            await page.CloseAsync().ConfigureAwait(false);
        }
    }
}
