using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using TUnit.Aspire.Http;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

[NotInParallel(nameof(BaggagePropagationHandlerTests))]
public class BaggagePropagationHandlerTests
{
    [Test]
    public async Task SendAsync_InjectsTraceparentHeader_WhenActivityExists()
    {
        Activity.Current = null;
        using var activity = new Activity("test-traceparent").Start();

        var captured = new CaptureHandler();
        var handler = CreateHandler();
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsTrue();
    }

    [Test]
    public async Task SendAsync_InjectsTraceContext_FromCreatedClientSpan_WhenHelperSpanIsCreated()
    {
        Activity.Current = null;
        using var listenerScope = new ActivityListenerScope();
        using var activity = new Activity("test-root").Start();
        activity.SetBaggage(TUnitActivitySource.TagTestId, "my-test-context-id");

        var captured = new CaptureHandler();
        var handler = CreateHandler();
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        var parts = traceparent.Split('-');
        var baggageHeader = captured.LastRequest.Headers.GetValues("baggage").First();
        var clientSpan = await Assert.That(listenerScope.StoppedActivities).HasSingleItem();

        await Assert.That(parts[1]).IsEqualTo(activity.TraceId.ToString());
        await Assert.That(parts[2]).IsEqualTo(clientSpan.SpanId);
        await Assert.That(parts[2]).IsNotEqualTo(activity.SpanId.ToString());
        await Assert.That(clientSpan.ParentSpanId).IsEqualTo(activity.SpanId.ToString());
        await Assert.That(clientSpan.Kind).IsEqualTo(ActivityKind.Client);
        await Assert.That(clientSpan.DisplayName).IsEqualTo("HTTP GET /test");
        await Assert.That(baggageHeader).Contains(TUnitActivitySource.TagTestId);
        await Assert.That(baggageHeader).Contains("my-test-context-id");
    }

    [Test]
    public async Task SendAsync_CreatesNewClientSpan_PerRequest()
    {
        Activity.Current = null;
        using var listenerScope = new ActivityListenerScope();
        using var activity = new Activity("test-multiple-requests").Start();

        var captured = new CaptureHandler();
        var handler = CreateHandler();
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test1");
        var traceparent1 = captured.LastRequest!.Headers.GetValues("traceparent").First();

        await client.GetAsync("http://localhost/test2");
        var traceparent2 = captured.LastRequest!.Headers.GetValues("traceparent").First();

        var spans = listenerScope.StoppedActivities;

        await Assert.That(spans.Length).IsEqualTo(2);
        await Assert.That(traceparent1.Split('-')[1]).IsEqualTo(activity.TraceId.ToString());
        await Assert.That(traceparent2.Split('-')[1]).IsEqualTo(activity.TraceId.ToString());
        await Assert.That(traceparent1.Split('-')[2]).IsEqualTo(spans[0].SpanId);
        await Assert.That(traceparent2.Split('-')[2]).IsEqualTo(spans[1].SpanId);
        await Assert.That(traceparent1.Split('-')[2]).IsNotEqualTo(traceparent2.Split('-')[2]);
    }

    [Test]
    public async Task SendAsync_FallsBackToActivityCurrent_WhenHelperSpanIsNotCreated()
    {
        Activity.Current = null;
        using var activity = new Activity("test-fallback").Start();
        activity.SetBaggage(TUnitActivitySource.TagTestId, "my-test-context-id");

        var captured = new CaptureHandler();
        var handler = CreateHandler(static _ => null);
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        var parts = traceparent.Split('-');
        var baggageHeader = captured.LastRequest.Headers.GetValues("baggage").First();

        await Assert.That(parts[1]).IsEqualTo(activity.TraceId.ToString());
        await Assert.That(parts[2]).IsEqualTo(activity.SpanId.ToString());
        await Assert.That(baggageHeader).Contains(TUnitActivitySource.TagTestId);
        await Assert.That(baggageHeader).Contains("my-test-context-id");
    }

    [Test]
    public async Task SendAsync_ClientSpan_RecordsResponseMetadata()
    {
        Activity.Current = null;
        using var listenerScope = new ActivityListenerScope();
        using var activity = new Activity("test-response-tags").Start();

        var captured = new CaptureHandler(HttpStatusCode.NotFound);
        var handler = CreateHandler();
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        await Assert.That((int)response.StatusCode).IsEqualTo(404);

        var clientSpan = await Assert.That(listenerScope.StoppedActivities).HasSingleItem();

        await Assert.That(clientSpan.Tags.GetValueOrDefault("http.request.method")).IsEqualTo("GET");
        await Assert.That(clientSpan.Tags.GetValueOrDefault("url.full")).IsEqualTo("http://localhost/test");
        await Assert.That(clientSpan.Tags.GetValueOrDefault("server.address")).IsEqualTo("localhost");
        await Assert.That(clientSpan.Tags.GetValueOrDefault("http.response.status_code")).IsEqualTo("404");
        await Assert.That(clientSpan.Status).IsEqualTo(ActivityStatusCode.Error);
    }

    [Test]
    public async Task SendAsync_InjectsBaggageHeader_WithActivityBaggage()
    {
        Activity.Current = null;
        using var activity = new Activity("test-inject-baggage").Start();
        activity.SetBaggage(TUnitActivitySource.TagTestId, "my-test-context-id");
        activity.SetBaggage("custom.key", "custom-value");

        var captured = new CaptureHandler();
        var handler = CreateHandler(static _ => null);
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("baggage")).IsTrue();

        var baggageHeader = captured.LastRequest.Headers.GetValues("baggage").First();
        await Assert.That(baggageHeader).Contains(TUnitActivitySource.TagTestId);
        await Assert.That(baggageHeader).Contains("my-test-context-id");
        await Assert.That(baggageHeader).Contains("custom.key");
        await Assert.That(baggageHeader).Contains("custom-value");
    }

    [Test]
    public async Task SendAsync_NoBaggage_DoesNotAddBaggageHeader()
    {
        Activity.Current = null;
        using var activity = new Activity("test-no-baggage").Start();

        var captured = new CaptureHandler();
        var handler = CreateHandler(static _ => null);
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_NoActivity_DoesNotInjectTraceContext()
    {
        Activity.Current = null;

        var captured = new CaptureHandler();
        var handler = CreateHandler(static _ => null);
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsFalse();
        await Assert.That(captured.LastRequest.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_BaggageValues_AreUriEncoded()
    {
        Activity.Current = null;
        using var activity = new Activity("test-encoding").Start();
        activity.SetBaggage("key with spaces", "value=with&special");

        var captured = new CaptureHandler();
        var handler = CreateHandler(static _ => null);
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var baggageHeader = captured.LastRequest!.Headers.GetValues("baggage").First();
        await Assert.That(baggageHeader).Contains("key%20with%20spaces");
        await Assert.That(baggageHeader).Contains("value%3Dwith%26special");
    }

    [Test]
    public async Task SendAsync_ExistingBaggageHeader_IsPreserved()
    {
        Activity.Current = null;
        using var activity = new Activity("test-existing-baggage").Start();
        activity.SetBaggage("should.not.appear", "true");

        var captured = new CaptureHandler();
        var handler = CreateHandler(static _ => null);
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.TryAddWithoutValidation("baggage", "existing=value");

        await client.SendAsync(request);

        var allBaggageValues = string.Join(",",
            captured.LastRequest!.Headers.GetValues("baggage"));
        await Assert.That(allBaggageValues).Contains("existing=value");
        await Assert.That(allBaggageValues).DoesNotContain("should.not.appear");
    }

    private static TUnitBaggagePropagationHandler CreateHandler(
        Func<HttpRequestMessage, Activity?>? startActivity = null)
    {
        return startActivity is null
            ? new TUnitBaggagePropagationHandler()
            : new TUnitBaggagePropagationHandler(startActivity);
    }

    private sealed class ActivityListenerScope : IDisposable
    {
        private readonly ConcurrentQueue<RecordedActivity> _stoppedActivities = new();
        private readonly ActivityListener _listener;

        public ActivityListenerScope()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = static source => source.Name == TUnitActivitySource.AspireHttpSourceName,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _stoppedActivities.Enqueue(new RecordedActivity(
                    activity.TraceId.ToString(),
                    activity.SpanId.ToString(),
                    activity.ParentSpanId == default ? null : activity.ParentSpanId.ToString(),
                    activity.DisplayName,
                    activity.Kind,
                    activity.Status,
                    activity.TagObjects.ToDictionary(static t => t.Key, static t => t.Value?.ToString())))
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public RecordedActivity[] StoppedActivities => _stoppedActivities.ToArray();

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record RecordedActivity(
        string TraceId,
        string SpanId,
        string? ParentSpanId,
        string DisplayName,
        ActivityKind Kind,
        ActivityStatusCode Status,
        IReadOnlyDictionary<string, string?> Tags);

    /// <summary>
    /// A handler that captures the outgoing request instead of sending it over the network.
    /// </summary>
    private sealed class CaptureHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
