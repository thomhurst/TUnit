using System.Runtime.InteropServices;
using Microsoft.Playwright;
using TUnit.Core;

namespace TUnit.Playwright;

public class PlaywrightSkipAttribute : SkipAttribute
{
    private readonly Targets[] _combinations;

    public TestContext? TestContext { get; set; }

    [Flags]
    public enum Targets : short
    {
        Windows = 1 << 0,
        Linux = 1 << 1,
        OSX = 1 << 2,
        Chromium = 1 << 3,
        Firefox = 1 << 4,
        Webkit = 1 << 5
    }

    /// <summary>
    /// Skips the combinations provided.
    /// </summary>
    /// <param name="combinations"></param>
    public PlaywrightSkipAttribute(params Targets[] combinations) : base("Skipped by browser/platform")
    {
        _combinations = combinations;
    }

    public override Task<bool> ShouldSkip(BeforeTestContext context)
    {
        var browserName = GetBrowserName(context.TestDetails);

        return Task.FromResult(_combinations.Any(combination =>
        {
            var requirements = Enum.GetValues<Targets>().Where(x => combination.HasFlag(x));
            return requirements.All(flag =>
                flag switch
                {
                    Targets.Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                    Targets.Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                    Targets.OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                    Targets.Chromium => browserName == BrowserType.Chromium,
                    Targets.Firefox => browserName == BrowserType.Firefox,
                    Targets.Webkit => browserName == BrowserType.Webkit,
                    _ => false,
                });
        }));
    }

    private static string? GetBrowserName(TestDetails testDetails)
    {
        if (testDetails.ClassInstance is PlaywrightTest playwrightTest)
        {
            return playwrightTest.BrowserName;
        }

        return null;
    }
}
