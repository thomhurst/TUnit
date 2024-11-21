using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TUnit.Playwright.Tests;

public class Tests : PageTest
{
    [Before(TestSession)]
    public static void InstallPlaywright()
    {
        if (Debugger.IsAttached)
        {
            Environment.SetEnvironmentVariable("PWDEBUG", "1");
        }
        
        Microsoft.Playwright.Program.Main(["install --with-deps"]);
    }
    
    [Test]
    public async Task Test()
    {
        await Page.GotoAsync("https://www.github.com/thomhurst/TUnit");
    }
    
    [Test]
    public async Task HomepageHasPlaywrightInTitleAndGetStartedLinkLinkingtoTheIntroPage()
    {
        await Page.GotoAsync("https://playwright.dev");

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));

        // create a locator
        var getStarted = Page.Locator("text=Get Started");

        // Expect an attribute "to be strictly equal" to the value.
        await Expect(getStarted).ToHaveAttributeAsync("href", "/docs/intro");

        // Click the get started link.
        await getStarted.ClickAsync();

        // Expects the URL to contain intro.
        await Expect(Page).ToHaveURLAsync(new Regex(".*intro"));
    }
}