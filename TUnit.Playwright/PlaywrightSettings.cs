using TUnit.Core.Settings;

namespace TUnit.Playwright;

public static class PlaywrightSettingsExtensions
{
    internal static readonly TUnitPlaywrightSettings Default = new();

    extension(TUnitSettings settings)
    {
        public TUnitPlaywrightSettings PlaywrightSettings => Default;
    }
}

public class TUnitPlaywrightSettings
{
    public bool DefaultHeadless { get; set; }
    public bool DefaultIgnoreHttpsErrors { get; set; }
}
