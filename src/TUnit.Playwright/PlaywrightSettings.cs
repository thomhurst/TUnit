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
}
