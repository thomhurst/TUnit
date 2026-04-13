using System.Diagnostics;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that propagates <c>traceparent</c>, <c>tracestate</c>, and
/// <c>baggage</c> W3C headers from <see cref="Activity.Current"/> onto outgoing
/// HTTP requests. .NET's <c>DiagnosticsHandler</c> (which injects <c>traceparent</c>)
/// is only present in the <see cref="HttpClient"/> pipeline when using
/// <c>IHttpClientFactory</c> — it is NOT wired in when constructing
/// <c>new HttpClient(handler)</c> directly. This handler fills that gap so that both
/// trace context and <c>tunit.test.id</c> baggage reach the SUT process.
/// </summary>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Activity.Current is { } activity)
        {
            // Inject traceparent and tracestate via the default propagator.
            // This is necessary because DiagnosticsHandler is not in the pipeline
            // when HttpClient is constructed with an explicit handler.
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

            // Inject baggage header if the propagator hasn't already done so.
            // When OTel SDK is configured with BaggagePropagator, the Inject call
            // above may have already added a baggage header — avoid duplicates.
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
