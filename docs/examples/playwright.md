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

## Recording Videos[​](#recording-videos "Direct link to Recording Videos")

Add `[RecordVideo]` to an individual test that inherits from `ContextTest` or `PageTest`, or uses per-test `ContextFixture` or `PageFixture` instances. Recording is enabled only for the selected test methods:

```
public class LoginPageTests : PageTest

{

    [Test]

    [RecordVideo]

    public async Task Login_Button_Is_Visible()

    {

        await Page.GotoAsync("https://example.com/login");



        var loginButton = Page.Locator("button#login");



        await Assert.That(await loginButton.IsVisibleAsync()).IsTrue();

    }

}
```

Once the test finishes, its recording is renamed to match the test (and attempt, if the test was retried) and attached to the test result, so it's easy to find in CI output alongside a dozen other recordings.

The same attribute works with composition:

```
public class CheckoutTests

{

    [ClassDataSource<PageFixture>]

    public required PageFixture BrowserPage { get; init; }



    [Test]

    [RecordVideo]

    public async Task Checkout_Page_Is_Visible()

    {

        await BrowserPage.Page.GotoAsync("https://example.com");

        await Assert.That(await BrowserPage.Page.Locator("body").IsVisibleAsync()).IsTrue();

    }

}
```

Keep `ContextFixture` and `PageFixture` private to each test with their default `SharedType.None`. The underlying `BrowserFixture` can still be shared. Recording fixtures shared between tests are rejected because their videos cannot be attributed reliably to one test.

With `[RecordVideo]`, each retry gets fresh contexts and pages before setup hooks run. Recordings are finalized after teardown hooks and attached with the attempt number. Multiple page fixtures and pages closed early are supported. Without the attribute, fixtures retain their normal lifetime across retries.

Overrides of `ContextFixture.GetContextOptions()` retain their custom options; recording settings are applied to a copy. If overriding fixture initialization or disposal, call the base implementation to preserve recording and cleanup.

Setting `RecordVideoDir` through `DefaultBrowserNewContextOptions` or `GetContextOptions()` without `[RecordVideo]` enables Playwright recording with the fixture's normal lifetime. These videos keep Playwright's filenames and are not automatically attached to a test result, because shared fixtures can record more than one test. Manage those artifacts yourself, or use `[RecordVideo]` with per-test fixtures for automatic naming and attachment.

Pass constructor arguments to control where recordings are written and the viewport size used while recording:

```
public class LoginPageTests : PageTest

{

    [Test]

    [RecordVideo(path: "videos", width: 1920, height: 1080)]

    public async Task Login_Button_Is_Visible()

    {

        await Page.GotoAsync("https://example.com/login");



        var loginButton = Page.Locator("button#login");



        await Assert.That(await loginButton.IsVisibleAsync()).IsTrue();

    }

}
```

* `path` - directory recordings are written to, resolved relative to the test application's working directory unless given as an absolute path. Defaults to `"playwright-artifacts"`.
* `width` - viewport width used for the recording, in pixels. Defaults to `1280`.
* `height` - viewport height used for the recording, in pixels. Defaults to `1400`.

For full Playwright API details, see the [Playwright for .NET documentation](https://playwright.dev/dotnet/).
