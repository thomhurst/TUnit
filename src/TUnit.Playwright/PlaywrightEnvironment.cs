using Microsoft.Playwright;

namespace TUnit.Playwright;

/// <summary>
/// Run-level switches read from environment variables, matching the Microsoft Playwright
/// test adapters: <c>HEADED=1</c> launches visible browsers and <c>BROWSER</c> picks the browser.
/// </summary>
/// <remarks>
/// Values are read when a browser launches, not cached, so a <c>[Before(TestSession)]</c> hook
/// can still set them.
/// </remarks>
internal static class PlaywrightEnvironment
{
    internal const string HeadedVariable = "HEADED";
    internal const string BrowserVariable = "BROWSER";

    /// <summary>
    /// The browser named by the <c>BROWSER</c> environment variable, or Chromium when it is unset.
    /// </summary>
    internal static string DefaultBrowserName => ResolveBrowserName(Environment.GetEnvironmentVariable(BrowserVariable));

    internal static string ResolveBrowserName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BrowserType.Chromium;
        }

        var name = value!.Trim();
        if (string.Equals(name, BrowserType.Chromium, StringComparison.OrdinalIgnoreCase))
        {
            return BrowserType.Chromium;
        }

        if (string.Equals(name, BrowserType.Firefox, StringComparison.OrdinalIgnoreCase))
        {
            return BrowserType.Firefox;
        }

        if (string.Equals(name, BrowserType.Webkit, StringComparison.OrdinalIgnoreCase))
        {
            return BrowserType.Webkit;
        }

        throw new InvalidOperationException(
            $"The {BrowserVariable} environment variable is '{value}'. Expected '{BrowserType.Chromium}', '{BrowserType.Firefox}' or '{BrowserType.Webkit}'.");
    }

    /// <summary>
    /// Applies the <c>HEADED</c> environment variable to <paramref name="options"/>.
    /// </summary>
    internal static BrowserTypeLaunchOptions ApplyHeaded(BrowserTypeLaunchOptions options) =>
        ApplyHeaded(options, Environment.GetEnvironmentVariable(HeadedVariable));

    /// <summary>
    /// Returns a copy of <paramref name="options"/> with <c>Headless = false</c> when
    /// <paramref name="headed"/> is <c>1</c> or <c>true</c> and the options leave
    /// <see cref="BrowserTypeLaunchOptions.Headless"/> unset; otherwise returns
    /// <paramref name="options"/> unchanged. An explicit <c>Headless</c> always wins, and the
    /// options are copied so shared settings objects are never mutated.
    /// </summary>
    internal static BrowserTypeLaunchOptions ApplyHeaded(BrowserTypeLaunchOptions options, string? headed)
    {
        if (options.Headless is not null || !IsEnabled(headed))
        {
            return options;
        }

        return new BrowserTypeLaunchOptions(options) { Headless = false };
    }

    private static bool IsEnabled(string? value)
    {
        var trimmed = value?.Trim();
        return trimmed == "1" || string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase);
    }
}
