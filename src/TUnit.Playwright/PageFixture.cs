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
public class PageFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<ContextFixture>]
    public required ContextFixture ContextFixture { get; init; }

    public IPage Page { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Page = await ContextFixture.Context.NewPageAsync().ConfigureAwait(false);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Page is not null)
        {
            await Page.CloseAsync().ConfigureAwait(false);
        }
    }
}
