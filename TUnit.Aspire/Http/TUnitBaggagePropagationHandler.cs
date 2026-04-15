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
            // New SpanId per request so the SUT's spans parent correctly within this trace
            var spanId = ActivitySpanId.CreateRandom();
            var sampled = activity.Recorded ? "01" : "00";
            request.Headers.TryAddWithoutValidation("traceparent",
                $"00-{activity.TraceId}-{spanId}-{sampled}");

            if (!request.Headers.Contains("baggage"))
            {
                var first = true;
                var sb = new System.Text.StringBuilder();

                foreach (var (key, value) in activity.Baggage)
                {
                    if (key is null)
                    {
                        continue;
                    }

                    if (!first)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Uri.EscapeDataString(key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(value ?? string.Empty));
                    first = false;
                }

                if (!first)
                {
                    request.Headers.TryAddWithoutValidation("baggage", sb.ToString());
                }
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
