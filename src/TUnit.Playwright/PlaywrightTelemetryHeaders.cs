using Microsoft.Playwright;

namespace TUnit.Playwright;

internal static class PlaywrightTelemetryHeaders
{
    public static BrowserNewContextOptions Merge(BrowserNewContextOptions options, bool propagate)
    {
#if NET
        if (!propagate || System.Diagnostics.Activity.Current is null)
        {
            return options;
        }

        // Seed user headers first so they win when the propagator tries to add the same key.
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (options.ExtraHTTPHeaders is not null)
        {
            foreach (var kvp in options.ExtraHTTPHeaders)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        var before = merged.Count;
        Telemetry.PlaywrightActivityPropagator.InjectInto(merged);
        if (merged.Count == before)
        {
            return options;
        }

        return new BrowserNewContextOptions(options) { ExtraHTTPHeaders = merged };
#else
        return options;
#endif
    }
}
