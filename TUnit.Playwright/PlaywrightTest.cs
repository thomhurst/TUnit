using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class PlaywrightTest : WorkerAwareTest
{
    public virtual string BrowserName { get; } = Microsoft.Playwright.BrowserType.Chromium;
    public IBrowserType BrowserType => Playwright[BrowserName];

    private static readonly Task<IPlaywright> PlaywrightTask = Microsoft.Playwright.Playwright.CreateAsync();

    public IPlaywright Playwright { get; private set; } = null!;

    [Before(HookType.Test)]
    public async Task PlaywrightSetup()
    {
        Playwright = await PlaywrightTask.ConfigureAwait(false);
        Playwright.Selectors.SetTestIdAttribute("data-testid");
    }

    public static void SetDefaultExpectTimeout(float timeout) => Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(timeout);

    public ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);

    public IPageAssertions Expect(IPage page) => Microsoft.Playwright.Assertions.Expect(page);

    public IAPIResponseAssertions Expect(IAPIResponse response) => Microsoft.Playwright.Assertions.Expect(response);
}
