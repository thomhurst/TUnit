using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class PlaywrightTest : WorkerAwareTest
{
    public virtual string BrowserName { get; } = Microsoft.Playwright.BrowserType.Chromium;
    public IBrowserType BrowserType => Playwright[BrowserName];

    private static readonly Task<IPlaywright> PlaywrightTask = Microsoft.Playwright.Playwright.CreateAsync();

    public static IPlaywright Playwright { get; private set; } = null!;

    [Before(HookType.TestSession)]
    public static async Task PlaywrightSetup()
    {
        Playwright = await PlaywrightTask.ConfigureAwait(false);
        Playwright.Selectors.SetTestIdAttribute("data-testid");
    }
    
    [After(HookType.TestSession)]
    public static void PlaywrightCleanup()
    {
        Playwright.Dispose();
    }

    public static void SetDefaultExpectTimeout(float timeout) => Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(timeout);

    public ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);

    public IPageAssertions Expect(IPage page) => Microsoft.Playwright.Assertions.Expect(page);

    public IAPIResponseAssertions Expect(IAPIResponse response) => Microsoft.Playwright.Assertions.Expect(response);
}
