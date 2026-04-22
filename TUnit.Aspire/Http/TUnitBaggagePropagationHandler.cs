using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that injects W3C <c>traceparent</c> and <c>baggage</c> headers into
/// outgoing requests made through <c>AspireFixture.CreateHttpClient</c>.
/// </summary>
/// <remarks>
/// Aspire's test <c>HttpClient</c> hits real sockets, so .NET's built-in
/// <c>System.Net.Http</c> ActivitySource already emits the outbound client span. This
/// handler only ensures trace context flows from the ambient test Activity onto the
/// outgoing request before that span starts, so the SUT can correlate requests to the
/// originating test.
/// </remarks>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpActivityPropagator.Inject(Activity.Current, request.Headers);
        return base.SendAsync(request, cancellationToken);
    }
}
