# Playwright

There is a NuGet package to help with Playwright: `TUnit.Playwright`

Once that is installed, a test can be as simple as:

```csharp
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

- `Page`
- `Context`
- `Browser`
- `Playwright`

## A Real Page Interaction Test

```csharp
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

## Configuring Browser Options

Override the `BrowserName` property to control which browser is launched. The possible values are:

- `chromium` (default)
- `firefox`
- `webkit`

Pass `BrowserTypeLaunchOptions` to the base constructor to configure headless mode, slow motion, and other launch settings:

```csharp
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

## Limiting Parallel Browser Tests

Browser tests can be resource-intensive. Use `[ParallelLimiter<T>]` to control how many run concurrently:

```csharp
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

## Recording Videos

Add `[RecordVideo]` to an individual test in a class that inherits from `ContextTest` or `PageTest` to record a video of every browser context it creates. Applying it per-test, rather than to the whole class, keeps recording (and the disk space and overhead it costs) limited to the tests that actually need it - such as ones you're debugging or that are flaky:

```csharp
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

Pass constructor arguments to control where recordings are written and the viewport size used while recording:

```csharp
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

- `path` - directory recordings are written to, resolved relative to the test application's working directory unless given as an absolute path. Defaults to `"playwright-artifacts"`.
- `width` - viewport width used for the recording, in pixels. Defaults to `1280`.
- `height` - viewport height used for the recording, in pixels. Defaults to `1400`.

For full Playwright API details, see the [Playwright for .NET documentation](https://playwright.dev/dotnet/).
