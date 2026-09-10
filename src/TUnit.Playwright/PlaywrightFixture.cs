using Microsoft.Playwright;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

public class PlaywrightFixture : IAsyncInitializer, IAsyncDisposable
{
    public IPlaywright Playwright { get; private set; } = null!;

    protected virtual string TestIdAttribute => "data-testid";

    public virtual async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        Playwright.Selectors.SetTestIdAttribute(TestIdAttribute);
    }

    public virtual ValueTask DisposeAsync()
    {
        Playwright?.Dispose();
        return default;
    }

    public ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);
    public IPageAssertions Expect(IPage page) => Microsoft.Playwright.Assertions.Expect(page);
    public IAPIResponseAssertions Expect(IAPIResponse response) => Microsoft.Playwright.Assertions.Expect(response);
}
