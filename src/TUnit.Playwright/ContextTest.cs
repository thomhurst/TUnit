using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class ContextTest : BrowserTest
{
    public ContextTest()
    {
    }

    public ContextTest(BrowserTypeLaunchOptions options) : base(options)
    {
    }

    public IBrowserContext Context { get; private set; } = null!;

    public virtual BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var configuredDefault = TUnitPlaywrightSettings.Default.DefaultBrowserNewContextOptions;
        var options = configuredDefault ?? new BrowserNewContextOptions
        {
            Locale = "en-US", ColorScheme = ColorScheme.Light,
        };

        if (testContext.StateBag.TryGetValue<RecordVideoAttribute>(RecordVideoAttribute.StateBagKey, out var recordVideo) &&
            recordVideo is not null)
        {
            // Never mutate TUnitPlaywrightSettings.Default.DefaultBrowserNewContextOptions in place:
            // it is a shared singleton reused across every test.
            if (configuredDefault is not null)
            {
                options = new BrowserNewContextOptions(configuredDefault);
            }

            options.RecordVideoDir = string.IsNullOrEmpty(recordVideo.Path)
                ? "playwright-artifacts"
                : recordVideo.Path;
            options.ViewportSize = new ViewportSize
            {
                Width = recordVideo.Width > 0 ? recordVideo.Width : 1280,
                Height = recordVideo.Height > 0 ? recordVideo.Height : 1400
            };
        }

        return options;
    }

    [Before(HookType.Test, "", 0)]
    public async Task ContextSetup(TestContext testContext)
    {
        if (Browser == null)
        {
            throw new InvalidOperationException($"Browser is not initialized. This may indicate that {nameof(BrowserTest)}.{nameof(BrowserSetup)} did not execute properly.");
        }

        Context = await NewContext(ContextOptions(testContext)).ConfigureAwait(false);
    }
}
