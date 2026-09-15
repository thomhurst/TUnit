using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

internal static class PlaywrightContextOptions
{
    internal static BrowserNewContextOptions Defaults() =>
        TUnitPlaywrightSettings.Default.DefaultBrowserNewContextOptions ?? new BrowserNewContextOptions
        {
            Locale = "en-US", ColorScheme = ColorScheme.Light,
        };

    internal static RecordVideoAttribute? Recording(TestContext? context) =>
        context is not null && context.StateBag.TryGetValue<RecordVideoAttribute>(RecordVideoAttribute.StateBagKey, out var recording)
            ? recording
            : null;

    internal static BrowserNewContextOptions ApplyRecording(BrowserNewContextOptions options, TestContext? context)
    {
        if (Recording(context) is not { } recording)
        {
            return options;
        }

        return new BrowserNewContextOptions(options)
        {
            RecordVideoDir = string.IsNullOrEmpty(recording.Path) ? "playwright-artifacts" : recording.Path,
            ViewportSize = new ViewportSize
            {
                Width = recording.Width > 0 ? recording.Width : 1280,
                Height = recording.Height > 0 ? recording.Height : 1400
            }
        };
    }
}
