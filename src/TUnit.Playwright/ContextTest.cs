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
            // it is a shared singleton reused across every test, and BrowserNewContextOptions has no Clone().
            if (configuredDefault is not null)
            {
                options = CloneOptions(configuredDefault);
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

    private static BrowserNewContextOptions CloneOptions(BrowserNewContextOptions source)
    {
        return new BrowserNewContextOptions
        {
            AcceptDownloads = source.AcceptDownloads,
            BaseURL = source.BaseURL,
            BypassCSP = source.BypassCSP,
            ClientCertificates = source.ClientCertificates,
            ColorScheme = source.ColorScheme,
            Contrast = source.Contrast,
            DeviceScaleFactor = source.DeviceScaleFactor,
            ExtraHTTPHeaders = source.ExtraHTTPHeaders,
            ForcedColors = source.ForcedColors,
            Geolocation = source.Geolocation,
            HasTouch = source.HasTouch,
            HttpCredentials = source.HttpCredentials,
            IgnoreHTTPSErrors = source.IgnoreHTTPSErrors,
            IsMobile = source.IsMobile,
            JavaScriptEnabled = source.JavaScriptEnabled,
            Locale = source.Locale,
            Offline = source.Offline,
            Permissions = source.Permissions,
            Proxy = source.Proxy,
            RecordHarContent = source.RecordHarContent,
            RecordHarMode = source.RecordHarMode,
            RecordHarOmitContent = source.RecordHarOmitContent,
            RecordHarPath = source.RecordHarPath,
            RecordHarUrlFilter = source.RecordHarUrlFilter,
            RecordHarUrlFilterRegex = source.RecordHarUrlFilterRegex,
            RecordHarUrlFilterString = source.RecordHarUrlFilterString,
            RecordVideoDir = source.RecordVideoDir,
            RecordVideoSize = source.RecordVideoSize,
            ReducedMotion = source.ReducedMotion,
            ScreenSize = source.ScreenSize,
            ServiceWorkers = source.ServiceWorkers,
            StorageState = source.StorageState,
            StorageStatePath = source.StorageStatePath,
            StrictSelectors = source.StrictSelectors,
            TimezoneId = source.TimezoneId,
            UserAgent = source.UserAgent,
            ViewportSize = source.ViewportSize,
        };
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
