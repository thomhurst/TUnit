using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that propagates W3C <c>traceparent</c> and <c>baggage</c> headers
/// from <see cref="Activity.Current"/> into outgoing HTTP requests. This enables natural
/// OpenTelemetry distributed tracing: the SUT receives the test's TraceId and all its
/// spans and logs can be correlated back to the originating test.
/// </summary>
/// <remarks>
/// Each test case starts its own W3C trace (unique TraceId) via a root activity in the
/// engine. HTTP requests made during a test inherit that TraceId through standard
/// <see cref="Activity.Current"/> propagation. The OTLP receiver maps the TraceId back
/// to the test via <see cref="TraceRegistry"/>, which is populated by the engine when
/// the test activity starts.
///
/// This handler is needed because Aspire's <c>CreateHttpClient</c> does not include
/// the standard <c>DiagnosticsHandler</c>, so W3C context propagation must be done
/// explicitly.
/// </remarks>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Activity.Current is { } activity)
        {
            // Aspire's CreateHttpClient doesn't create an outgoing client span. Propagate the
            // current test span itself so downstream server spans parent to a real exported span
            // instead of a synthetic parent ID that backends can't render.
            DistributedContextPropagator.Current.Inject(activity, request, static (carrier, key, value) =>
            {
                if (carrier is HttpRequestMessage httpRequest && key is not null && !httpRequest.Headers.Contains(key))
                {
                    httpRequest.Headers.TryAddWithoutValidation(key, value);
                }
            });

            if (!request.Headers.Contains("baggage")
                && TUnitActivitySource.TryBuildBaggageHeader(activity) is { } baggage)
            {
                // Older target frameworks still default to Correlation-Context for baggage.
                // Emit W3C baggage explicitly so backend correlation is stable everywhere.
                request.Headers.TryAddWithoutValidation("baggage", baggage);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
