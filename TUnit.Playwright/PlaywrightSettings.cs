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

    public bool? DefaultHeadless { get; set; } = null;
    public bool DefaultIgnoreHttpsErrors { get; set; }
}
