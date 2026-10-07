using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NSubstitute;
using TUnit.Playwright;

namespace TUnit.UnitTests;

public class PlaywrightTraceContextTests
{
    [Test]
    [Arguments("https://app.test", true)]
    [Arguments("https://app.test/", true)]
    [Arguments("https://app.test/page?q=1", true)]
    [Arguments("https://app.test?q=1", true)]
    [Arguments("https://APP.test/page", true)]
    [Arguments("https://app.test.evil/page", false)]
    [Arguments("https://app.test:8443/page", false)]
    [Arguments("http://app.test/page", false)]
    [Arguments("https://fonts.gstatic.com/s/font.woff2", false)]
    public async Task OriginPatternMatchesOnlyConfiguredOrigin(string url, bool expected)
    {
        var pattern = PlaywrightTraceContextRoute.BuildOriginPattern(["https://app.test/ignored/path"], null)!;

        await Assert.That(pattern.IsMatch(url)).IsEqualTo(expected);
    }

    [Test]
    public async Task OriginPatternNormalizesDefaultPortAndMatchesEveryOrigin()
    {
        var pattern = PlaywrightTraceContextRoute.BuildOriginPattern(["https://app.test:443", "http://localhost:5000"], null)!;

        await Assert.That(pattern.IsMatch("https://app.test/")).IsTrue();
        await Assert.That(pattern.IsMatch("http://localhost:5000/api")).IsTrue();
        await Assert.That(pattern.IsMatch("http://localhost:5001/api")).IsFalse();
    }

    [Test]
    public async Task OriginPatternFallsBackToBaseUrl()
    {
        var pattern = PlaywrightTraceContextRoute.BuildOriginPattern(null, "https://localhost:7217/app/")!;

        await Assert.That(pattern.IsMatch("https://localhost:7217/other")).IsTrue();
        await Assert.That(pattern.IsMatch("https://fonts.gstatic.com/")).IsFalse();
    }

    [Test]
    public async Task OriginPatternIsNullWithoutOrigins()
    {
        await Assert.That(PlaywrightTraceContextRoute.BuildOriginPattern(null, null)).IsNull();
        await Assert.That(PlaywrightTraceContextRoute.BuildOriginPattern([], "https://app.test")).IsNull();
    }

    [Test]
    [Arguments("not a url")]
    [Arguments("/relative")]
    [Arguments("file:///C:/app")]
    public async Task OriginPatternRejectsInvalidOrigins(string origin)
    {
        await Assert.That(() => PlaywrightTraceContextRoute.BuildOriginPattern([origin], null))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task NewContextRoutesTraceHeadersToConfiguredOriginsOnly()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        var test = new OriginsBrowserTest(["https://app.test"]) { Browser = browser };

        await test.NewContext(new BrowserNewContextOptions());

        await browser.Received(1).NewContextAsync(Arg.Is<BrowserNewContextOptions>(o => o.ExtraHTTPHeaders == null));
        var (pattern, handler) = CapturedRoute(context);
        await Assert.That(pattern.IsMatch("https://app.test/")).IsTrue();
        await Assert.That(pattern.IsMatch("https://fonts.gstatic.com/")).IsFalse();

        var route = CreateRoute(new Dictionary<string, string> { ["accept"] = "text/html" });
        await handler(route);

        var headers = FallbackHeaders(route);
        await Assert.That(headers["accept"]).IsEqualTo("text/html");
        await Assert.That(headers["traceparent"]).Contains(activity.TraceId.ToHexString());
    }

    [Test]
    public async Task RoutedRequestKeepsExistingTraceparent()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        var test = new OriginsBrowserTest(["https://app.test"]) { Browser = browser };

        await test.NewContext(new BrowserNewContextOptions());
        var route = CreateRoute(new Dictionary<string, string> { ["traceparent"] = "from-page" });
        await CapturedRoute(context).Handler(route);

        await Assert.That(FallbackHeaders(route)["traceparent"]).IsEqualTo("from-page");
    }

    [Test]
    public async Task NewContextUsesBaseUrlOriginByDefault()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        var test = new BrowserTest { Browser = browser };

        await test.NewContext(new BrowserNewContextOptions { BaseURL = "https://localhost:7217" });

        var (pattern, _) = CapturedRoute(context);
        await Assert.That(pattern.IsMatch("https://localhost:7217/login")).IsTrue();
        await Assert.That(pattern.IsMatch("https://fonts.gstatic.com/")).IsFalse();
    }

    [Test]
    public async Task NewContextSendsNoTraceHeadersWithoutKnownOrigin()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        var test = new BrowserTest { Browser = browser };

        await test.NewContext(new BrowserNewContextOptions());

        await browser.Received(1).NewContextAsync(Arg.Is<BrowserNewContextOptions>(o => o.ExtraHTTPHeaders == null));
        await context.DidNotReceiveWithAnyArgs().RouteAsync(default(Regex)!, default(Func<IRoute, Task>)!);
    }

    [Test]
    public async Task NewContextSendsNoTraceHeadersWhenDisabled()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        var test = new OriginsBrowserTest(["https://app.test"], propagate: false) { Browser = browser };

        await test.NewContext(new BrowserNewContextOptions());

        await context.DidNotReceiveWithAnyArgs().RouteAsync(default(Regex)!, default(Func<IRoute, Task>)!);
    }

    [Test]
    public async Task ContextFixtureRoutesTraceHeadersToConfiguredOrigins()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        var fixture = new OriginsContextFixture
        {
            BrowserFixture = new BrowserFixture { Browser = browser, PlaywrightFixture = null! }
        };

        await fixture.InitializeAsync();

        var (pattern, handler) = CapturedRoute(context);
        await Assert.That(pattern.IsMatch("https://app.test/")).IsTrue();
        await Assert.That(pattern.IsMatch("https://fonts.gstatic.com/")).IsFalse();

        var route = CreateRoute([]);
        await handler(route);
        await Assert.That(FallbackHeaders(route)["traceparent"]).Contains(activity.TraceId.ToHexString());
    }

    [Test]
    public async Task ContextIsClosedWhenRouteRegistrationFails()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        context.WhenForAnyArgs(c => c.RouteAsync(default(Regex)!, default(Func<IRoute, Task>)!))
            .Do(_ => throw new PlaywrightException("route failed"));
        var test = new OriginsBrowserTest(["https://app.test"]) { Browser = browser };

        await Assert.That(() => test.NewContext(new BrowserNewContextOptions())).Throws<PlaywrightException>();
        await context.Received(1).CloseAsync();
    }

    [Test]
    public async Task ContextFixtureClosesContextWhenRouteRegistrationFails()
    {
        using var activity = new Activity("trace-context-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (browser, context) = CreateBrowser();
        context.WhenForAnyArgs(c => c.RouteAsync(default(Regex)!, default(Func<IRoute, Task>)!))
            .Do(_ => throw new PlaywrightException("route failed"));
        var fixture = new OriginsContextFixture
        {
            BrowserFixture = new BrowserFixture { Browser = browser, PlaywrightFixture = null! }
        };

        await Assert.That(fixture.InitializeAsync).Throws<PlaywrightException>();
        await context.Received(1).CloseAsync();
        await Assert.That(fixture.Context).IsNull();
    }

    private static (IBrowser Browser, IBrowserContext Context) CreateBrowser()
    {
        var browser = Substitute.For<IBrowser>();
        var context = Substitute.For<IBrowserContext>();
        context.Pages.Returns(Array.Empty<IPage>());
        browser.NewContextAsync(Arg.Any<BrowserNewContextOptions>()).Returns(context);
        return (browser, context);
    }

    private static (Regex Pattern, Func<IRoute, Task> Handler) CapturedRoute(IBrowserContext context)
    {
        var call = context.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(IBrowserContext.RouteAsync));
        var arguments = call.GetArguments();
        return ((Regex)arguments[0]!, (Func<IRoute, Task>)arguments[1]!);
    }

    private static IRoute CreateRoute(Dictionary<string, string> headers)
    {
        var request = Substitute.For<IRequest>();
        request.Headers.Returns(headers);
        var route = Substitute.For<IRoute>();
        route.Request.Returns(request);
        return route;
    }

    private static Dictionary<string, string> FallbackHeaders(IRoute route) =>
        ((RouteFallbackOptions)route.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IRoute.FallbackAsync))
            .GetArguments()[0]!).Headers!
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

    private sealed class OriginsBrowserTest(IReadOnlyList<string> origins, bool propagate = true) : BrowserTest
    {
        public override bool PropagateTraceContext => propagate;

        public override IReadOnlyList<string>? TraceContextOrigins => origins;
    }

    private sealed class OriginsContextFixture : ContextFixture
    {
        protected override IReadOnlyList<string>? TraceContextOrigins => ["https://app.test"];
    }
}
