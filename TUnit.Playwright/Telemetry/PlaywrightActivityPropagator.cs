#if NET

using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Playwright.Telemetry;

/// <summary>
/// Injects W3C <c>traceparent</c>/<c>baggage</c> headers from <see cref="Activity.Current"/>
/// into a Playwright <see cref="Microsoft.Playwright.BrowserNewContextOptions.ExtraHTTPHeaders"/>
/// dictionary. Pre-existing keys in the target are preserved.
/// </summary>
internal static class PlaywrightActivityPropagator
{
    public static void InjectInto(IDictionary<string, string> headers)
    {
        if (Activity.Current is not { } activity)
        {
            return;
        }

        DistributedContextPropagator.Current.Inject(activity, headers, static (carrier, key, value) =>
        {
            if (carrier is IDictionary<string, string> dict && key is not null && value is not null)
            {
                dict.TryAdd(key, value);
            }
        });

        if (TUnitActivitySource.TryBuildBaggageHeader(activity) is { } baggage)
        {
            headers.TryAdd(TUnitActivitySource.BaggageHeader, baggage);
        }
    }
}

#endif
