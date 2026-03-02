using System.Diagnostics;

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

            // Inject W3C traceparent header so the server creates child activities under the same trace
            request.Headers.Remove("traceparent");
            request.Headers.TryAddWithoutValidation("traceparent",
                $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}");
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
