using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

public class ContextFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<BrowserFixture>(Shared = SharedType.PerTestSession)]
    public required BrowserFixture BrowserFixture { get; init; }

    public IBrowserContext Context { get; private set; } = null!;

    protected virtual BrowserNewContextOptions GetContextOptions() =>
        new() { Locale = "en-US", ColorScheme = ColorScheme.Light };

    /// <summary>
    /// When <c>true</c>, seeds the context with W3C trace propagation headers from
    /// the current test's <see cref="System.Diagnostics.Activity"/>.
    /// </summary>
    protected virtual bool PropagateTraceContext => true;

    public virtual async Task InitializeAsync()
    {
        var options = PlaywrightTelemetryHeaders.Merge(GetContextOptions(), PropagateTraceContext);
        Context = await BrowserFixture.Browser.NewContextAsync(options).ConfigureAwait(false);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.CloseAsync().ConfigureAwait(false);
        }
    }
}
