using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that injects W3C <c>traceparent</c> and <c>baggage</c> headers
/// into outgoing HTTP requests for cross-process OTLP correlation. Each request gets
/// a unique TraceId so the SUT's logs can be routed back to the specific test, even
/// when the engine's test activities share a class-level TraceId.
/// </summary>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Generate a unique TraceId per request. The engine creates test activities
        // as children of the class activity, so all tests in a class share the same
        // TraceId. Using a fresh TraceId here ensures the OTLP receiver can map each
        // request's logs back to the originating test, not just the class.
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        request.Headers.TryAddWithoutValidation("traceparent", $"00-{traceId}-{spanId}-01");

        // Register the unique TraceId so the OTLP receiver can correlate logs
        if (TestContext.Current is { } testContext)
        {
            TraceRegistry.Register(
                traceId.ToString(),
                testContext.TestDetails.TestId,
                testContext.Id);
        }

        // Propagate baggage (including tunit.test.id) from the current activity
        if (Activity.Current is { } activity && !request.Headers.Contains("baggage"))
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
