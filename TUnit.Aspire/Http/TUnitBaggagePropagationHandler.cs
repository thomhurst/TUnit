using System.Diagnostics;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that propagates W3C <c>traceparent</c> and <c>baggage</c> headers
/// from <see cref="Activity.Current"/> onto outgoing HTTP requests. The TUnit engine gives
/// each test its own unique TraceId, so propagating the current activity's context is
/// sufficient for per-test OTLP log correlation.
/// </summary>
/// <remarks>
/// .NET's built-in <c>DiagnosticsHandler</c> (which injects <c>traceparent</c>) is only
/// present in the <see cref="HttpClient"/> pipeline when using <c>IHttpClientFactory</c>.
/// This handler fills that gap for <c>new HttpClient(handler)</c> scenarios.
/// </remarks>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Activity.Current is { } activity)
        {
            // Inject traceparent and tracestate via the default propagator.
            DistributedContextPropagator.Current.Inject(
                activity,
                request,
                static (carrier, key, value) =>
                {
                    if (carrier is HttpRequestMessage req)
                    {
                        req.Headers.TryAddWithoutValidation(key, value);
                    }
                });

            // Inject baggage if the propagator hasn't already done so.
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
