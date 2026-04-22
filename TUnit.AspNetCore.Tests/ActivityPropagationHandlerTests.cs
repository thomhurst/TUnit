using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

[NotInParallel(nameof(ActivityPropagationHandlerTests))]
public class ActivityPropagationHandlerTests
{
    [Test]
    public async Task SendAsync_InjectsTraceContext_WhenHelperSpanIsCreated()
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

        await Assert.That(parts.Length).IsEqualTo(4);
        await Assert.That(parts[1]).IsEqualTo(activity.TraceId.ToString());
        await Assert.That(parts[2]).IsEqualTo(clientSpan.SpanId);
        await Assert.That(parts[2]).IsNotEqualTo(activity.SpanId.ToString());
        await Assert.That(clientSpan.DisplayName).IsEqualTo("GET");
        await Assert.That(baggageHeader).Contains(TUnitActivitySource.TagTestId);
        await Assert.That(baggageHeader).Contains("my-test-context-id");
    }

    [Test]
    public async Task SendAsync_FallsBackToActivityCurrent_WhenHelperSpanIsNotCreated()
    {
        Activity.Current = null;
        using var activity = new Activity("test-root").Start();
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
    public async Task SendAsync_DoesNotInjectTraceContext_WhenNoAmbientActivityExists()
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
    public async Task SendAsync_ClientSpan_3xxStatus_LeavesStatusUnset()
    {
        Activity.Current = null;
        using var listenerScope = new ActivityListenerScope();
        using var activity = new Activity("test-redirect").Start();

        var captured = new CaptureHandler(HttpStatusCode.Redirect);
        var handler = CreateHandler();
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        await Assert.That((int)response.StatusCode).IsEqualTo(302);

        var clientSpan = await Assert.That(listenerScope.StoppedActivities).HasSingleItem();
        await Assert.That(clientSpan.Tags.GetValueOrDefault("http.response.status_code")).IsEqualTo("302");
        await Assert.That(clientSpan.Tags.ContainsKey("error.type")).IsFalse();
        await Assert.That(clientSpan.Status).IsEqualTo(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task SendAsync_ClientSpan_4xxStatus_SetsErrorStatus()
    {
        Activity.Current = null;
        using var listenerScope = new ActivityListenerScope();
        using var activity = new Activity("test-not-found").Start();

        var captured = new CaptureHandler(HttpStatusCode.NotFound);
        var handler = CreateHandler();
        handler.InnerHandler = captured;
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        await Assert.That((int)response.StatusCode).IsEqualTo(404);

        var clientSpan = await Assert.That(listenerScope.StoppedActivities).HasSingleItem();
        await Assert.That(clientSpan.Tags.GetValueOrDefault("http.response.status_code")).IsEqualTo("404");
        await Assert.That(clientSpan.Tags.GetValueOrDefault("error.type")).IsEqualTo("404");
        await Assert.That(clientSpan.Status).IsEqualTo(ActivityStatusCode.Error);
    }

    [Test]
    public async Task SendAsync_ClientSpan_RecordsException_WhenInnerHandlerThrows()
    {
        Activity.Current = null;
        using var listenerScope = new ActivityListenerScope();
        using var activity = new Activity("test-transport-error").Start();

        var handler = CreateHandler();
        handler.InnerHandler = new ThrowingHandler(new HttpRequestException("boom"));
        using var client = new HttpClient(handler);

        HttpRequestException? thrown = null;
        try
        {
            await client.GetAsync("http://localhost/test");
        }
        catch (HttpRequestException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();

        var clientSpan = await Assert.That(listenerScope.StoppedActivities).HasSingleItem();
        await Assert.That(clientSpan.Tags.GetValueOrDefault("error.type")).Contains(nameof(HttpRequestException));
        await Assert.That(clientSpan.EventNames).Contains("exception");
        await Assert.That(clientSpan.Status).IsEqualTo(ActivityStatusCode.Error);
    }

    // Pass static _ => null to simulate no helper span; null uses the real StartHttpActivity default.
    private static DelegatingHandler CreateHandler(Func<HttpRequestMessage, Activity?>? startActivity = null)
    {
        return startActivity is null
            ? new ActivityPropagationHandler()
            : new ActivityPropagationHandler(startActivity);
    }

    private sealed class ActivityListenerScope : IDisposable
    {
        private readonly ConcurrentQueue<RecordedActivity> _stoppedActivities = new();
        private readonly ActivityListener _listener;

        public ActivityListenerScope()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = static source => source.Name == TUnitActivitySource.AspNetCoreHttpSourceName,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _stoppedActivities.Enqueue(new RecordedActivity(
                    activity.SpanId.ToString(),
                    activity.DisplayName,
                    activity.Status,
                    activity.TagObjects.ToDictionary(static t => t.Key, static t => t.Value?.ToString()),
                    activity.Events.Select(static e => e.Name).ToArray()))
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
        string SpanId,
        string DisplayName,
        ActivityStatusCode Status,
        IReadOnlyDictionary<string, string?> Tags,
        string[] EventNames);

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public CaptureHandler(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _statusCode = statusCode;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }
}
