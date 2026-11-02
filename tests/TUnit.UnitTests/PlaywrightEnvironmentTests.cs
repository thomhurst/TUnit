using Microsoft.Playwright;
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
}
