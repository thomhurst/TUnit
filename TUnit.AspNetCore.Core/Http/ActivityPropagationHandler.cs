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
    private readonly Func<HttpRequestMessage, Activity?> _startActivity;

    public ActivityPropagationHandler()
    {
        _startActivity = StartHttpActivity;
    }

    internal ActivityPropagationHandler(Func<HttpRequestMessage, Activity?> startActivity)
    {
        _startActivity = startActivity;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ambientActivity = Activity.Current;
        using var activity = _startActivity(request);

        if (activity is not null)
        {
            activity.SetTag("http.request.method", request.Method.Method);
            activity.SetTag("url.full", request.RequestUri?.ToString());
            activity.SetTag("server.address", request.RequestUri?.Host);

            // WebApplicationFactory bypasses DiagnosticsHandler, so when we synthesize
            // a client span we also need to flow the ambient baggage onto it explicitly.
            // Child Activities do not reliably surface parent baggage across all target
            // frameworks, but correlation relies on the test's baggage being propagated.
            CopyBaggage(ambientActivity, activity);
        }

        // Propagate the current distributed trace even when the helper span is not
        // created (for example, when no listener is attached to TUnit.AspNetCore.Http).
        var propagationActivity = activity ?? ambientActivity;
        InjectTraceContext(propagationActivity, request.Headers);
        InjectBaggage(propagationActivity, request.Headers);

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            throw;
        }

        if (activity is not null)
        {
            var statusCode = (int)response.StatusCode;
            activity.SetTag("http.response.status_code", statusCode);
            if (statusCode >= 400)
            {
                activity.SetStatus(ActivityStatusCode.Error);
                activity.SetTag("error.type", statusCode.ToString());
            }
        }

        return response;
    }

    private static Activity? StartHttpActivity(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? request.RequestUri?.ToString() ?? "unknown";
        return HttpActivitySource.StartActivity(
            $"HTTP {request.Method} {path}",
            ActivityKind.Client);
    }

    private static void InjectTraceContext(Activity? activity, HttpRequestHeaders headers)
    {
        if (activity is null)
        {
            return;
        }

        // Inject trace context headers (traceparent + tracestate) so the server
        // creates child activities under the same trace. Respect pre-existing headers
        // so callers who explicitly set their own context win.
        DistributedContextPropagator.Current.Inject(activity, headers,
            static (targetHeaders, key, value) =>
            {
                if (targetHeaders is HttpRequestHeaders h && key is not null && !h.Contains(key))
                {
                    h.TryAddWithoutValidation(key, value);
                }
            });
    }

    private static void CopyBaggage(Activity? source, Activity destination)
    {
        if (source is null || ReferenceEquals(source, destination))
        {
            return;
        }

        foreach (var (key, value) in source.Baggage)
        {
            if (key is null || destination.GetBaggageItem(key) is not null)
            {
                continue;
            }

            destination.SetBaggage(key, value);
        }
    }

    private static void InjectBaggage(Activity? activity, HttpRequestHeaders headers)
    {
        // If a propagator already emitted W3C baggage (e.g. OTel SDK's BaggagePropagator),
        // preserve it; otherwise emit our own so LegacyPropagator-based stacks still
        // propagate test correlation baggage.
        if (activity is null || headers.Contains(TUnit.Core.TUnitActivitySource.BaggageHeader))
        {
            return;
        }

        if (TUnit.Core.TUnitActivitySource.TryBuildBaggageHeader(activity) is { } baggage)
        {
            headers.TryAddWithoutValidation(TUnit.Core.TUnitActivitySource.BaggageHeader, baggage);
        }
    }

    private static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.ToString() }
        };

        activity.AddEvent(new ActivityEvent("exception", tags: tags));
        activity.SetTag("error.type", exception.GetType().FullName);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
