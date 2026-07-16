using System.Diagnostics;
using TUnit.Core;

namespace TUnit.AspNetCore;

/// <summary>
/// DelegatingHandler that injects W3C <c>traceparent</c> and <c>baggage</c> headers into
/// outgoing requests so the SUT can correlate them to the originating test.
/// </summary>
/// <remarks>
/// No client Activity is created here. For in-memory <c>WebApplicationFactory</c> traffic the
/// ASP.NET Core server span becomes a direct child of the ambient test Activity — no synthetic
/// client span is needed to stitch the trace. For SUT-initiated <c>IHttpClientFactory</c>
/// pipelines, the runtime's <c>System.Net.Http</c> ActivitySource already emits a properly-shaped
/// client span.
/// </remarks>
internal sealed class ActivityPropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpActivityPropagator.Inject(Activity.Current, request.Headers);
        return base.SendAsync(request, cancellationToken);
    }
}
