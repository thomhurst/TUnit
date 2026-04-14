using System.Diagnostics;
using TUnit.Core;

namespace TUnit.Aspire.Http;

/// <summary>
/// DelegatingHandler that injects W3C <c>traceparent</c> and <c>baggage</c> headers
/// into outgoing HTTP requests for cross-process OTLP correlation. Each request gets
/// a unique TraceId so the SUT's logs can be routed back to the specific test, even
/// though all tests in a class share the class activity's TraceId.
/// </summary>
/// <remarks>
/// The engine keeps test activities as children of the class activity (parent-child,
/// shared TraceId) so trace backends display proper waterfalls:
/// Session → Assembly → Class → Test₁, Test₂, ...
/// This handler generates a fresh TraceId per outbound request and registers it in
/// <see cref="TraceRegistry"/> so the OTLP receiver can correlate SUT logs back to
/// the originating test.
/// </remarks>
internal sealed class TUnitBaggagePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Generate a unique TraceId per request so the OTLP receiver can map each
        // request's logs back to the originating test. The engine's test activities
        // share the class-level TraceId (for proper trace-backend waterfall display),
        // so we need a distinct TraceId here for per-test correlation.
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
