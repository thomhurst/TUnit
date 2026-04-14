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
        using var activity = new Activity("test-inject-traceparent").Start();

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsTrue();
    }

    [Test]
    public async Task SendAsync_TraceparentContainsActivityTraceId()
    {
        using var activity = new Activity("test-traceid-match").Start();
        var activityTraceId = activity.TraceId.ToString();

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        // traceparent format: 00-{traceId}-{spanId}-{flags}
        await Assert.That(traceparent).Contains(activityTraceId);
    }

    [Test]
    public async Task SendAsync_InjectsBaggageHeader_WithActivityBaggage()
    {
        using var activity = new Activity("test-inject-baggage").Start();
        activity.SetBaggage("tunit.test.id", "my-test-context-id");
        activity.SetBaggage("custom.key", "custom-value");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("baggage")).IsTrue();

        var baggageHeader = captured.LastRequest.Headers.GetValues("baggage").First();
        await Assert.That(baggageHeader).Contains("tunit.test.id");
        await Assert.That(baggageHeader).Contains("my-test-context-id");
        await Assert.That(baggageHeader).Contains("custom.key");
        await Assert.That(baggageHeader).Contains("custom-value");
    }

    [Test]
    public async Task SendAsync_NoBaggage_DoesNotAddBaggageHeader()
    {
        // Detach from engine's activity to prevent inheriting tunit.test.id baggage
        Activity.Current = null;
        using var activity = new Activity("test-no-baggage").Start();

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_NoActivity_DoesNotInjectHeaders()
    {
        Activity.Current = null;

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest).IsNotNull();
        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsFalse();
        await Assert.That(captured.LastRequest.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_BaggageValues_AreUriEncoded()
    {
        // Detach from engine's activity to test encoding in isolation
        Activity.Current = null;
        using var activity = new Activity("test-encoding").Start();
        activity.SetBaggage("key with spaces", "value=with&special");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var baggageHeader = captured.LastRequest!.Headers.GetValues("baggage").First();
        // URI-encoded keys and values
        await Assert.That(baggageHeader).Contains("key%20with%20spaces");
        await Assert.That(baggageHeader).Contains("value%3Dwith%26special");
    }

    [Test]
    public async Task SendAsync_MultipleBaggageItems_CommaSeparated()
    {
        using var activity = new Activity("test-multi-baggage").Start();
        activity.SetBaggage("key1", "val1");
        activity.SetBaggage("key2", "val2");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var baggageHeader = captured.LastRequest!.Headers.GetValues("baggage").First();
        // Should contain comma separator between items
        await Assert.That(baggageHeader).Contains(",");
    }

    [Test]
    public async Task SendAsync_ExistingBaggageHeader_IsPreserved()
    {
        using var activity = new Activity("test-existing-baggage").Start();
        activity.SetBaggage("should.not.appear", "true");

        var captured = new CaptureHandler();
        var handler = new TUnitBaggagePropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.TryAddWithoutValidation("baggage", "existing=value");

        await client.SendAsync(request);

        // The existing baggage value must still be present regardless of propagator behavior
        var allBaggageValues = string.Join(",",
            captured.LastRequest!.Headers.GetValues("baggage"));
        await Assert.That(allBaggageValues).Contains("existing=value");
    }

    [Test]
    public async Task SendAsync_InnerHandlerResponse_IsPassedThrough()
    {
        using var activity = new Activity("test-passthrough").Start();

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
