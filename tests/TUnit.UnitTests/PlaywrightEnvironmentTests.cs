using Microsoft.Playwright;
using NSubstitute;
using TUnit.Playwright;

namespace TUnit.UnitTests;

public class PlaywrightEnvironmentTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("  ")]
    public async Task UnsetBrowserDefaultsToChromium(string? value)
    {
        await Assert.That(PlaywrightEnvironment.ResolveBrowserName(value)).IsEqualTo(BrowserType.Chromium);
    }

    [Test]
    [Arguments("chromium", "chromium")]
    [Arguments("Firefox", "firefox")]
    [Arguments(" WEBKIT ", "webkit")]
    public async Task BrowserNamesAreNormalized(string value, string expected)
    {
        await Assert.That(PlaywrightEnvironment.ResolveBrowserName(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task UnknownBrowserThrowsNamingTheVariable()
    {
        await Assert.That(() => PlaywrightEnvironment.ResolveBrowserName("edge"))
            .Throws<InvalidOperationException>()
            .WithMessageContaining("BROWSER");
    }

    [Test]
    [Arguments("1")]
    [Arguments("true")]
    [Arguments(" TRUE ")]
    public async Task HeadedLaunchesVisibleBrowserWhenHeadlessIsUnset(string headed)
    {
        var options = new BrowserTypeLaunchOptions { SlowMo = 100 };

        var applied = PlaywrightEnvironment.ApplyHeaded(options, headed);

        await Assert.That(applied.Headless).IsEqualTo(false);
        await Assert.That(applied.SlowMo).IsEqualTo(100);
        // The caller's options, which may be the shared settings object, are not mutated.
        await Assert.That(options.Headless).IsNull();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("0")]
    [Arguments("false")]
    public async Task HeadedUnsetOrDisabledLeavesOptionsUnchanged(string? headed)
    {
        var options = new BrowserTypeLaunchOptions();

        var applied = PlaywrightEnvironment.ApplyHeaded(options, headed);

        await Assert.That(applied).IsSameReferenceAs(options);
        await Assert.That(applied.Headless).IsNull();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ExplicitHeadlessWinsOverHeaded(bool headless)
    {
        var options = new BrowserTypeLaunchOptions { Headless = headless };

        var applied = PlaywrightEnvironment.ApplyHeaded(options, "1");

        await Assert.That(applied).IsSameReferenceAs(options);
        await Assert.That(applied.Headless).IsEqualTo(headless);
    }

    [Test]
    [NotInParallel(nameof(PlaywrightEnvironmentTests))]
    public async Task BrowserFixtureLaunchReadsBrowserAndHeadedAtLaunch()
    {
        var launch = new LaunchCapture();
        var fixture = new BrowserFixture { PlaywrightFixture = launch.CreatePlaywrightFixture() };

        // Set after construction: the variables are read when the browser launches.
        using (new EnvironmentScope(("BROWSER", "Firefox"), ("HEADED", "1")))
        {
            await fixture.InitializeAsync();
        }

        await Assert.That(launch.BrowserName).IsEqualTo(BrowserType.Firefox);
        await Assert.That(launch.Options!.Headless).IsEqualTo(false);
    }

    [Test]
    [NotInParallel(nameof(PlaywrightEnvironmentTests))]
    public async Task BrowserFixtureOverridesWinOverEnvironment()
    {
        var launch = new LaunchCapture();
        var fixture = new PinnedBrowserFixture { PlaywrightFixture = launch.CreatePlaywrightFixture() };

        using (new EnvironmentScope(("BROWSER", "firefox"), ("HEADED", "1")))
        {
            await fixture.InitializeAsync();
        }

        await Assert.That(launch.BrowserName).IsEqualTo(BrowserType.Webkit);
        await Assert.That(launch.Options!.Headless).IsEqualTo(true);
    }

    [Test]
    [NotInParallel(nameof(PlaywrightEnvironmentTests))]
    public async Task BrowserFixtureWithoutVariablesLaunchesDefaultChromium()
    {
        var launch = new LaunchCapture();
        var fixture = new BrowserFixture { PlaywrightFixture = launch.CreatePlaywrightFixture() };

        using (new EnvironmentScope(("BROWSER", null), ("HEADED", null)))
        {
            await fixture.InitializeAsync();
        }

        await Assert.That(launch.BrowserName).IsEqualTo(BrowserType.Chromium);
        await Assert.That(launch.Options!.Headless).IsNull();
    }

    [Test]
    [NotInParallel(nameof(PlaywrightEnvironmentTests))]
    public async Task BrowserTestReadsBrowserAndHeadedAfterConstruction()
    {
        var options = new BrowserTypeLaunchOptions { SlowMo = 50 };
        var test = new BrowserTest(options);

        using (new EnvironmentScope(("BROWSER", "webkit"), ("HEADED", "true")))
        {
            await Assert.That(test.BrowserName).IsEqualTo(BrowserType.Webkit);
            var launched = test.ResolveLaunchOptions();
            await Assert.That(launched.Headless).IsEqualTo(false);
            await Assert.That(launched.SlowMo).IsEqualTo(50);
        }

        await Assert.That(options.Headless).IsNull();
    }

    [Test]
    [NotInParallel(nameof(PlaywrightEnvironmentTests))]
    public async Task BrowserTestExplicitHeadlessWinsOverHeaded()
    {
        var test = new BrowserTest(new BrowserTypeLaunchOptions { Headless = true });

        using (new EnvironmentScope(("HEADED", "1")))
        {
            await Assert.That(test.ResolveLaunchOptions().Headless).IsEqualTo(true);
        }
    }

    private sealed class PinnedBrowserFixture : BrowserFixture
    {
        public override string BrowserName => BrowserType.Webkit;

        protected override BrowserTypeLaunchOptions GetLaunchOptions() => new() { Headless = true };
    }

    private sealed class LaunchCapture
    {
        public string? BrowserName { get; private set; }
        public BrowserTypeLaunchOptions? Options { get; private set; }

        public PlaywrightFixture CreatePlaywrightFixture()
        {
            var playwright = Substitute.For<IPlaywright>();
            playwright[Arg.Any<string>()].Returns(call =>
            {
                BrowserName = call.Arg<string>();
                var browserType = Substitute.For<IBrowserType>();
                browserType.LaunchAsync(Arg.Any<BrowserTypeLaunchOptions?>()).Returns(launchCall =>
                {
                    Options = launchCall.Arg<BrowserTypeLaunchOptions?>();
                    return Task.FromResult(Substitute.For<IBrowser>());
                });
                return browserType;
            });
            return new PlaywrightFixture { Playwright = playwright };
        }
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly (string Name, string? Value)[] _previous;

        public EnvironmentScope(params (string Name, string? Value)[] variables)
        {
            // Launch must not divert to the remote service, which ignores launch options.
            variables = [.. variables, ("PLAYWRIGHT_SERVICE_ACCESS_TOKEN", null), ("PLAYWRIGHT_SERVICE_URL", null)];
            _previous = variables.Select(v => (v.Name, Environment.GetEnvironmentVariable(v.Name))).ToArray();
            foreach (var (name, value) in variables)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
