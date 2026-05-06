using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

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
