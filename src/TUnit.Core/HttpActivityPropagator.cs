#if NET

using System.Diagnostics;
using System.Net.Http.Headers;

namespace TUnit.Core;

/// <summary>
/// Injects W3C <c>traceparent</c> and <c>baggage</c> headers onto outgoing HTTP requests
/// so a remote process (SUT) can correlate them to the originating test.
/// </summary>
/// <remarks>
/// Pre-existing headers always win — callers who explicitly set their own trace context
/// or baggage are not overridden. Baggage is also emitted when the configured
/// <see cref="DistributedContextPropagator"/> doesn't emit it itself (e.g. the default
/// LegacyPropagator emits Correlation-Context instead).
/// </remarks>
internal static class HttpActivityPropagator
{
    internal static void Inject(Activity? activity, HttpRequestHeaders headers)
    {
        if (activity is null)
        {
            return;
        }

        DistributedContextPropagator.Current.Inject(activity, headers, static (carrier, key, value) =>
        {
            if (carrier is HttpRequestHeaders h && key is not null && !h.Contains(key))
            {
                h.TryAddWithoutValidation(key, value);
            }
        });

        if (headers.Contains(TUnitActivitySource.BaggageHeader))
        {
            return;
        }

        if (TUnitActivitySource.TryBuildBaggageHeader(activity) is { } baggage)
        {
            headers.TryAddWithoutValidation(TUnitActivitySource.BaggageHeader, baggage);
        }
    }
}

#endif
