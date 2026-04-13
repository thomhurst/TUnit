using System.Diagnostics;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that serializes <see cref="Activity.Current"/> baggage items
/// into the W3C <c>baggage</c> HTTP header. .NET's default
/// <see cref="DistributedContextPropagator"/> injects <c>traceparent</c> and
/// <c>tracestate</c> but does NOT inject <c>baggage</c>. This handler fills that gap
/// so that <c>tunit.test.id</c> (and any other baggage) reaches the SUT process.
/// </summary>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Activity.Current is { } activity)
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

        return base.SendAsync(request, cancellationToken);
    }
}
