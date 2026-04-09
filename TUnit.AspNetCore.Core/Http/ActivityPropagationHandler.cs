using System.Diagnostics;
using System.Net.Http.Headers;

namespace TUnit.AspNetCore;

/// <summary>
/// DelegatingHandler that creates Activity spans for HTTP requests and propagates
/// trace context via the W3C traceparent header. This bridges the gap where
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
/// creates an HttpClient with an in-memory handler, bypassing .NET's built-in
/// DiagnosticsHandler that normally creates HTTP Activity spans.
/// </summary>
internal sealed class ActivityPropagationHandler : DelegatingHandler
{
    // Intentionally process-scoped: lives for the test process lifetime and is
    // cleaned up on process exit. Not disposed explicitly because multiple handler
    // instances share this source across concurrent tests.
    private static readonly ActivitySource HttpActivitySource = new("TUnit.AspNetCore.Http");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? request.RequestUri?.ToString() ?? "unknown";
        using var activity = HttpActivitySource.StartActivity(
            $"HTTP {request.Method} {path}",
            ActivityKind.Client);

        if (activity is not null)
        {
            activity.SetTag("http.request.method", request.Method.Method);
            activity.SetTag("url.full", request.RequestUri?.ToString());
            activity.SetTag("server.address", request.RequestUri?.Host);

            // Inject trace context headers (traceparent + tracestate) so the server
            // creates child activities under the same trace
            DistributedContextPropagator.Current.Inject(activity, request.Headers,
                static (headers, key, value) =>
                {
                    if (headers is HttpRequestHeaders h)
                    {
                        h.Remove(key);
                        h.TryAddWithoutValidation(key, value);
                    }
                });
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (activity is not null)
        {
            activity.SetTag("http.response.status_code", (int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                activity.SetStatus(ActivityStatusCode.Error);
            }
        }

        return response;
    }
}
