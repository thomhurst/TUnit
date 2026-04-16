using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

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

        await Assert.That(parts[1]).IsEqualTo(activity.TraceId.ToString());
        await Assert.That(parts[2]).IsNotEqualTo(activity.SpanId.ToString());
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

    private static DelegatingHandler CreateHandler(Func<HttpRequestMessage, Activity?>? startActivity = null)
    {
        var activityPropagationHandlerType = typeof(TUnitTestIdHandler).Assembly
            .GetType("TUnit.AspNetCore.ActivityPropagationHandler", throwOnError: true)!;

        var instance = startActivity is null
            ? Activator.CreateInstance(activityPropagationHandlerType, nonPublic: true)
            : Activator.CreateInstance(
                activityPropagationHandlerType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: [startActivity],
                culture: null);

        return (DelegatingHandler)instance!;
    }

    private sealed class ActivityListenerScope : IDisposable
    {
        private readonly ActivityListener _listener = new()
        {
            ShouldListenTo = static source => source.Name == "TUnit.AspNetCore.Http",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };

        public ActivityListenerScope()
        {
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
