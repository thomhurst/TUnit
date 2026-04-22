using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that creates Aspire HTTP client spans and propagates W3C
/// <c>traceparent</c> and <c>baggage</c> headers into outgoing HTTP requests.
/// This restores normal OpenTelemetry client/server span topology for
/// <c>AspireFixture.CreateHttpClient</c> requests.
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
    private static readonly ActivitySource HttpActivitySource = new(TUnitActivitySource.AspireHttpSourceName);
    private readonly Func<HttpRequestMessage, Activity?> _startActivity;

    public TUnitBaggagePropagationHandler()
    {
        _startActivity = StartHttpActivity;
    }

    internal TUnitBaggagePropagationHandler(Func<HttpRequestMessage, Activity?> startActivity)
    {
        _startActivity = startActivity;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ambientActivity = Activity.Current;
        using var activity = _startActivity(request);

        if (activity is not null)
        {
            activity.SetTag("http.request.method", request.Method.Method);
            activity.SetTag("url.full", request.RequestUri?.ToString());
            activity.SetTag("server.address", request.RequestUri?.Host);

            // Aspire's CreateHttpClient bypasses DiagnosticsHandler, so when we synthesize
            // a client span we also need to flow the ambient baggage onto it explicitly.
            CopyBaggage(ambientActivity, activity);
        }

        var propagationActivity = activity ?? ambientActivity;
        InjectTraceContext(propagationActivity, request);
        InjectBaggage(propagationActivity, request);

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            TUnitActivitySource.RecordException(activity, ex);
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

    private static void InjectTraceContext(Activity? activity, HttpRequestMessage request)
    {
        if (activity is null)
        {
            return;
        }

        DistributedContextPropagator.Current.Inject(activity, request, static (carrier, key, value) =>
        {
            if (carrier is HttpRequestMessage httpRequest && key is not null && !httpRequest.Headers.Contains(key))
            {
                httpRequest.Headers.TryAddWithoutValidation(key, value);
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

    private static void InjectBaggage(Activity? activity, HttpRequestMessage request)
    {
        if (activity is null || request.Headers.Contains(TUnitActivitySource.BaggageHeader))
        {
            return;
        }

        if (TUnitActivitySource.TryBuildBaggageHeader(activity) is { } baggage)
        {
            // Belt-and-braces for users who opt out of TUnit's W3C propagator alignment
            // via TUNIT_KEEP_LEGACY_PROPAGATOR=1: LegacyPropagator emits Correlation-Context
            // only, so still emit W3C baggage explicitly for backend correlation.
            request.Headers.TryAddWithoutValidation(TUnitActivitySource.BaggageHeader, baggage);
        }
    }
}
