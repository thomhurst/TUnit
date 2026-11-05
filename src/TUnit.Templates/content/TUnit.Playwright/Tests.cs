using System.Text.RegularExpressions;
using TUnit.Playwright;

namespace TestProject;

public class Tests : PageTest
{
    [Test]
    public async Task Test()
    {
        // Serve sample pages in the browser so this test works without a public website.
        await Page.RouteAsync("https://tunit-playwright.test/**", route =>
            route.FulfillAsync(new()
            {
                ContentType = "text/html",
                Body = """
                    <!doctype html>
                    <html lang="en">
                        <head><title>TUnit Playwright</title></head>
                        <body><a href="/docs/intro">Get Started</a></body>
                    </html>
                    """,
            }));

        await Page.GotoAsync("https://tunit-playwright.test/");

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
