using System.Diagnostics;
using TUnit.Aspire.Http;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class BaggagePropagationHandlerTests
{
    [Test]
    public async Task SendAsync_InjectsTraceparentHeader()
    {
        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsTrue();
    }

    [Test]
    public async Task SendAsync_TraceparentUsesUniqueTraceId_NotActivityCurrent()
    {
        using var activity = new Activity("test-unique-traceid").Start();
        var activityTraceId = activity.TraceId.ToString();

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        var parts = traceparent.Split('-');
        var requestTraceId = parts[1];

        // The handler generates its own TraceId, distinct from Activity.Current's
        await Assert.That(requestTraceId).IsNotEqualTo(activityTraceId);
    }

    [Test]
    public async Task SendAsync_EachRequestGetsUniqueTraceId()
    {
        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test1");
        var traceparent1 = captured.LastRequest!.Headers.GetValues("traceparent").First();

        await client.GetAsync("http://localhost/test2");
        var traceparent2 = captured.LastRequest!.Headers.GetValues("traceparent").First();

        var traceId1 = traceparent1.Split('-')[1];
        var traceId2 = traceparent2.Split('-')[1];

        await Assert.That(traceId1).IsNotEqualTo(traceId2);
    }

    [Test]
    public async Task SendAsync_TraceparentFormat_IsValidW3C()
    {
        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        var parts = traceparent.Split('-');

        await Assert.That(parts.Length).IsEqualTo(4);
        await Assert.That(parts[0]).IsEqualTo("00");           // version
        await Assert.That(parts[1].Length).IsEqualTo(32);      // trace-id (16 bytes hex)
        await Assert.That(parts[2].Length).IsEqualTo(16);      // parent-id (8 bytes hex)
        await Assert.That(parts[3]).IsEqualTo("01");           // flags (sampled)
    }

    [Test]
    public async Task SendAsync_RegistersTraceIdInTraceRegistry()
    {
        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        var traceId = traceparent.Split('-')[1];

        await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
    }

    [Test]
    public async Task SendAsync_InjectsBaggageHeader_WithActivityBaggage()
    {
        // Detach from engine activity to control baggage precisely
        Activity.Current = null;
        using var activity = new Activity("test-inject-baggage").Start();
        activity.SetBaggage(TUnitActivitySource.TagTestId, "my-test-context-id");
        activity.SetBaggage("custom.key", "custom-value");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
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
        // Detach from engine activity to prevent inheriting tunit.test.id baggage
        Activity.Current = null;
        using var activity = new Activity("test-no-baggage").Start();

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_NoActivity_StillInjectsTraceparent()
    {
        Activity.Current = null;

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest).IsNotNull();
        // traceparent is always injected (handler generates its own TraceId)
        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsTrue();
        // No Activity means no baggage to propagate
        await Assert.That(captured.LastRequest.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_BaggageValues_AreUriEncoded()
    {
        Activity.Current = null;
        using var activity = new Activity("test-encoding").Start();
        activity.SetBaggage("key with spaces", "value=with&special");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var baggageHeader = captured.LastRequest!.Headers.GetValues("baggage").First();
        await Assert.That(baggageHeader).Contains("key%20with%20spaces");
        await Assert.That(baggageHeader).Contains("value%3Dwith%26special");
    }

    [Test]
    public async Task SendAsync_MultipleBaggageItems_CommaSeparated()
    {
        Activity.Current = null;
        using var activity = new Activity("test-multi-baggage").Start();
        activity.SetBaggage("key1", "val1");
        activity.SetBaggage("key2", "val2");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var baggageHeader = captured.LastRequest!.Headers.GetValues("baggage").First();
        await Assert.That(baggageHeader).Contains(",");
    }

    [Test]
    public async Task SendAsync_ExistingBaggageHeader_IsPreserved()
    {
        Activity.Current = null;
        using var activity = new Activity("test-existing-baggage").Start();
        activity.SetBaggage("should.not.appear", "true");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.TryAddWithoutValidation("baggage", "existing=value");

        await client.SendAsync(request);

        var allBaggageValues = string.Join(",",
            captured.LastRequest!.Headers.GetValues("baggage"));
        await Assert.That(allBaggageValues).Contains("existing=value");
    }

    [Test]
    public async Task SendAsync_InnerHandlerResponse_IsPassedThrough()
    {
        var captured = new CaptureHandler(System.Net.HttpStatusCode.NotFound);
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        await Assert.That((int)response.StatusCode).IsEqualTo(404);
    }

    /// <summary>
    /// A handler that captures the outgoing request instead of sending it over the network.
    /// </summary>
    private sealed class CaptureHandler(
        System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) : HttpMessageHandler
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
