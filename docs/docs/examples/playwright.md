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

Add `[RecordVideo]` to an individual test that inherits from `ContextTest` or `PageTest`, or uses per-test `ContextFixture` or `PageFixture` instances. Recording is enabled only for the selected test methods:

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

The same attribute works with composition:

```csharp
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

## Screenshots and Traces on Failure

Add `[CaptureOnFailure]` to capture a full-page screenshot of each open page and a [Playwright trace](https://playwright.dev/dotnet/docs/trace-viewer) when a test fails, times out or is cancelled. Both files are attached to the test result. Passing and skipped tests discard the trace and write no files.

```csharp
[CaptureOnFailure]
public class LoginPageTests : PageTest
{
    [Test]
    public async Task Login_Button_Is_Visible()
    {
        await Page.GotoAsync("https://example.com/login");

        await Assert.That(await Page.Locator("button#login").IsVisibleAsync()).IsTrue();
    }
}
```

The attribute can go on a method, a class or an assembly (`[assembly: CaptureOnFailure]`); the closest one applies. It works for contexts created by `ContextTest`, `PageTest` and per-test `ContextFixture` and `PageFixture` instances.

Files are named `<test id hash>_<display name>.png` and `<test id hash>_<display name>.trace.zip`. The hash comes from the unique test id, so parallel tests in one class and `[Arguments]` variants of one method never write to the same file. Retries add `-attemptN`, and a test with more than one open page gets one screenshot per page (`-page1`, `-page2`). Open a trace with `pwsh bin/Debug/netX/playwright.ps1 show-trace <file>` or at [trace.playwright.dev](https://trace.playwright.dev).

```csharp
public class CheckoutTests : PageTest
{
    [Test]
    [CaptureOnFailure(path: "failures", Trace = false)]
    public async Task Checkout_Page_Is_Visible()
    {
        await Page.GotoAsync("https://example.com/checkout");

        await Assert.That(await Page.Locator("body").IsVisibleAsync()).IsTrue();
    }
}
```

- `path` - directory files are written to, resolved relative to the test application's working directory unless given as an absolute path. Defaults to `"playwright-artifacts"`.
- `Screenshot` - take a full-page screenshot of each open page. Defaults to `true`.
- `Trace` - record a trace with screenshots, DOM snapshots and sources. Defaults to `true`.

Capture runs after your `[After(Test)]` hooks and before the context closes, so a test whose body passes but whose teardown hook throws is captured too. As with `[RecordVideo]`, captured `ContextFixture` and `PageFixture` instances are created again for each retry attempt. Fixtures shared between tests are skipped, because no single test owns them. Fixture capture needs .NET 8 or later; on .NET Framework (the `netstandard2.0` build) only `ContextTest` and `PageTest` are captured, because TUnit cannot tell whether a fixture is shared there. If a screenshot or trace cannot be saved, for example because the browser crashed, the reason is written to the test's error output and the test result is not changed.

## Shared Browser Crashes

When a shared browser disconnects without TUnit closing it, for example because it crashed, every test that uses it fails with `TargetClosedException: Target page, context or browser has been closed`. TUnit records when the browser disconnected and reports it on each affected test:

- A fixture that cannot create its context or page fails with `The shared chromium browser disconnected at 2026-10-31 12:01:03.412 UTC, before this failure...`, with the Playwright error as the inner exception.
- A test that fails with a Playwright error after the disconnect gets the same note in its error output.

`BrowserFixture.DisconnectedAt` exposes the time. To keep a long run going after a crash, let the shared `BrowserFixture` launch a new browser for the next context it creates:

```csharp
public class PlaywrightSetup
{
    [Before(HookType.TestDiscovery)]
    public static Task Configure(BeforeTestDiscoveryContext context)
    {
        context.Settings.PlaywrightSettings.RelaunchDisconnectedBrowser = true;

        return Task.CompletedTask;
    }
}
```

Tests that inherit `BrowserTest`, `ContextTest` or `PageTest` already get a new browser after a failed test.

## Trace Context Propagation

TUnit can add W3C `traceparent` and `baggage` headers from the current test to the requests your page sends to the application under test. This lets the application's telemetry be correlated with the test that triggered it.

Headers are sent only to the origins under test. By default this is the origin of `BaseURL` in the context options. If no base URL is set, no headers are sent. List the origins explicitly with `TraceContextOrigins`:

```csharp
public class DashboardTests : PageTest
{
    public override IReadOnlyList<string>? TraceContextOrigins => ["https://localhost:7217"];

    [Test]
    public async Task Dashboard_Loads()
    {
        await Page.GotoAsync("https://localhost:7217/dashboard");
        await Assert.That(await Page.TitleAsync()).IsEqualTo("Dashboard");
    }
}
```

`ContextFixture` has the same property as a `protected virtual` member. Override `PropagateTraceContext` to return `false` to disable propagation.

Requests to other origins, such as web fonts or third-party APIs, never receive the headers. `traceparent` is not a CORS-safelisted header, so sending it to a third-party origin causes a CORS preflight that most servers reject, and it also leaks the test's trace id.

The headers are added by a context route, which has these limits:

- Playwright disables its HTTP cache for contexts with routes. Set `PropagateTraceContext` to `false` to keep the cache.
- Routes you add later run before TUnit's route. Call `route.FallbackAsync()` instead of `route.ContinueAsync()` in them to keep the headers.
- Context routes do not see WebSocket traffic or requests handled by a service worker, so those requests get no headers. To route requests that a service worker would handle, set `ServiceWorkers = ServiceWorkerPolicy.Block` in the context options.
- When a request to an origin under test is redirected, Playwright keeps the headers on the redirected request, even if it goes to another origin.

For full Playwright API details, see the [Playwright for .NET documentation](https://playwright.dev/dotnet/).
