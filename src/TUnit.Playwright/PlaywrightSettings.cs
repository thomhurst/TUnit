using Microsoft.Playwright;
using TUnit.Core.Settings;

namespace TUnit.Playwright;

public static class PlaywrightSettingsExtensions
{
    extension(TUnitSettings settings)
    {
        public TUnitPlaywrightSettings PlaywrightSettings => TUnitPlaywrightSettings.Default;
    }
}

public class TUnitPlaywrightSettings
{
    internal static readonly TUnitPlaywrightSettings Default = new();

    internal TUnitPlaywrightSettings()
    {
    }

    /// <summary>
    /// Options used when launching the browser for tests inheriting <see cref="BrowserTest"/>
    /// or <see cref="BrowserFixture"/>. When non-null, this fully replaces the hardcoded defaults.
    /// </summary>
    public BrowserTypeLaunchOptions? DefaultBrowserTypeLaunchOptions { get; set; } = null;

    /// <summary>
    /// Options used when creating a browser context for tests inheriting <see cref="ContextTest"/>
    /// or <see cref="ContextFixture"/>. When non-null, this fully replaces the hardcoded defaults
    /// (<c>Locale = "en-US"</c>, <c>ColorScheme = Light</c>).
    /// </summary>
    public BrowserNewContextOptions? DefaultBrowserNewContextOptions { get; set; } = null;

    /// <summary>
    /// When <c>true</c>, a shared <see cref="BrowserFixture"/> whose browser disconnected without
    /// TUnit closing it (for example because it crashed) launches a new browser for the next
    /// context it creates, so later tests in a long run can still pass. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The fixture relaunches by calling <see cref="BrowserFixture.InitializeAsync"/> again, so
    /// overrides of that method run again too. Tests that inherit <see cref="BrowserTest"/>
    /// already get a new browser after a failed test.
    /// </remarks>
    public bool RelaunchDisconnectedBrowser { get; set; }
}
