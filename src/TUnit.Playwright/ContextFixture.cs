using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

/// <summary>
/// The injected <see cref="BrowserFixture"/> is hardcoded to <see cref="SharedType.PerTestSession"/>.
/// Authoring a new fixture class is the way to change that scope — attribute arguments on
/// inherited <c>init</c> properties cannot be overridden.
/// </summary>
public class ContextFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<BrowserFixture>(Shared = SharedType.PerTestSession)]
    public required BrowserFixture BrowserFixture { get; init; }

    public IBrowserContext Context { get; private set; } = null!;

    /// <summary>
    /// Returns the options used when creating each <see cref="IBrowserContext"/>. Defaults
    /// match <see cref="ContextTest.ContextOptions"/> — pinned <c>Locale = "en-US"</c> and
    /// <c>ColorScheme = Light</c> for deterministic cross-platform rendering. Override to
    /// match your application's locale or to restore browser-default behaviour
    /// (<c>new BrowserNewContextOptions()</c>).
    /// </summary>
    protected virtual BrowserNewContextOptions GetContextOptions() =>
        TUnitPlaywrightSettings.Default.DefaultBrowserNewContextOptions ?? new BrowserNewContextOptions
        {
            Locale = "en-US", ColorScheme = ColorScheme.Light,
        };

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
