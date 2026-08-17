# Playwright

There is a NuGet package to help with Playwright: `TUnit.Playwright`

Once that is installed, a test can be as simple as:

```
public class Tests : PageTest

{

    [Test]

    public async Task Test()

    {

        await Page.GotoAsync("https://www.github.com/thomhurst/TUnit");

    }

}
```

By inheriting from `PageTest`, the base class handles setting up and disposing your playwright objects for you.

The following properties are available to use:

* `Page`
* `Context`
* `Browser`
* `Playwright`

## A Real Page Interaction Test[​](#a-real-page-interaction-test "Direct link to A Real Page Interaction Test")

```
public class LoginPageTests : PageTest

{

    [Test]

    public async Task Login_Button_Is_Visible()

    {

        await Page.GotoAsync("https://example.com/login");



        var loginButton = Page.Locator("button#login");



        await Assert.That(await loginButton.IsVisibleAsync()).IsTrue();

    }



    [Test]

    public async Task Successful_Login_Redirects_To_Dashboard()

    {

        await Page.GotoAsync("https://example.com/login");



        await Page.FillAsync("#username", "testuser");

        await Page.FillAsync("#password", "password123");

        await Page.ClickAsync("button#login");



        await Page.WaitForURLAsync("**/dashboard");



        var heading = await Page.Locator("h1").TextContentAsync();



        await Assert.That(heading).IsEqualTo("Dashboard");

    }

}
```

## Configuring Browser Options[​](#configuring-browser-options "Direct link to Configuring Browser Options")

Override the `BrowserName` property to control which browser is launched. The possible values are:

* `chromium` (default)
* `firefox`
* `webkit`

Pass `BrowserTypeLaunchOptions` to the base constructor to configure headless mode, slow motion, and other launch settings:

```
public class HeadlessChromeTests : PageTest

{

    public HeadlessChromeTests() : base(new BrowserTypeLaunchOptions

    {

        Headless = true,

        SlowMo = 100 // adds 100ms delay between actions for debugging

    })

    {

    }



    public override string BrowserName => "chromium";



    [Test]

    public async Task Page_Title_Matches()

    {

        await Page.GotoAsync("https://example.com");



        var title = await Page.TitleAsync();



        await Assert.That(title).Contains("Example");

    }

}
```

## Limiting Parallel Browser Tests[​](#limiting-parallel-browser-tests "Direct link to Limiting Parallel Browser Tests")

Browser tests can be resource-intensive. Use `[ParallelLimiter<T>]` to control how many run concurrently:

```
public class BrowserParallelLimit : IParallelLimit

{

    public int Limit => 2;

}



[ParallelLimiter<BrowserParallelLimit>]

public class HeavyBrowserTests : PageTest

{

    [Test]

    public async Task Test_A()

    {

        await Page.GotoAsync("https://example.com/a");

        await Assert.That(await Page.TitleAsync()).IsNotNull();

    }



    [Test]

    public async Task Test_B()

    {

        await Page.GotoAsync("https://example.com/b");

        await Assert.That(await Page.TitleAsync()).IsNotNull();

    }

}
```

This ensures at most 2 tests from this class run at the same time, preventing browser resource exhaustion.

For full Playwright API details, see the [Playwright for .NET documentation](https://playwright.dev/dotnet/).
