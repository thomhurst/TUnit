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
    public async Task SendAsync_InjectsTraceparent_FromAmbientActivity()
    {
        Activity.Current = null;
        using var activity = new Activity("test-root").Start();

        var captured = new CaptureHandler();
        var handler = new ActivityPropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var traceparent = captured.LastRequest!.Headers.GetValues("traceparent").First();
        var parts = traceparent.Split('-');

        await AssertValidW3CTraceparent(traceparent);
        await Assert.That(parts[1]).IsEqualTo(activity.TraceId.ToString());
        // No synthesized client span — traceparent's parent-id is the ambient activity itself.
        await Assert.That(parts[2]).IsEqualTo(activity.SpanId.ToString());
    }

    [Test]
    public async Task SendAsync_InjectsBaggage_FromAmbientActivity()
    {
        Activity.Current = null;
        using var activity = new Activity("test-root").Start();
        activity.SetBaggage(TUnitActivitySource.TagTestId, "my-test-context-id");
        activity.SetBaggage("custom.key", "custom-value");

        var captured = new CaptureHandler();
        var handler = new ActivityPropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        var baggageHeader = captured.LastRequest!.Headers.GetValues("baggage").First();
        await Assert.That(baggageHeader).Contains(TUnitActivitySource.TagTestId);
        await Assert.That(baggageHeader).Contains("my-test-context-id");
        await Assert.That(baggageHeader).Contains("custom.key");
        await Assert.That(baggageHeader).Contains("custom-value");
    }

    [Test]
    public async Task SendAsync_NoAmbientActivity_InjectsNothing()
    {
        Activity.Current = null;

        var captured = new CaptureHandler();
        var handler = new ActivityPropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        await Assert.That(captured.LastRequest!.Headers.Contains("traceparent")).IsFalse();
        await Assert.That(captured.LastRequest.Headers.Contains("baggage")).IsFalse();
    }

    [Test]
    public async Task SendAsync_ForwardsInnerHandlerResponse()
    {
        Activity.Current = null;
        using var activity = new Activity("test-status").Start();

        var captured = new CaptureHandler(HttpStatusCode.NotFound);
        var handler = new ActivityPropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        await Assert.That((int)response.StatusCode).IsEqualTo(404);
    }

    [Test]
    public async Task SendAsync_PropagatesInnerHandlerException()
    {
        Activity.Current = null;
        using var activity = new Activity("test-transport-error").Start();

        var handler = new ActivityPropagationHandler
        {
            InnerHandler = new ThrowingHandler(new HttpRequestException("boom")),
        };
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
        await Assert.That(thrown!.Message).IsEqualTo("boom");
    }

    [Test]
    public async Task SendAsync_ExistingBaggageHeader_IsPreserved()
    {
        Activity.Current = null;
        using var activity = new Activity("test-existing-baggage").Start();
        activity.SetBaggage("should.not.appear", "true");

        var captured = new CaptureHandler();
        var handler = new ActivityPropagationHandler { InnerHandler = captured };
        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.TryAddWithoutValidation("baggage", "existing=value");

        await client.SendAsync(request);

        var allBaggageValues = string.Join(",",
            captured.LastRequest!.Headers.GetValues("baggage"));
        await Assert.That(allBaggageValues).Contains("existing=value");
        await Assert.That(allBaggageValues).DoesNotContain("should.not.appear");
    }

    private static async Task AssertValidW3CTraceparent(string traceparent)
    {
        var parts = traceparent.Split('-');

        await Assert.That(parts.Length).IsEqualTo(4);
        await Assert.That(parts[0]).IsEqualTo("00");
        await Assert.That(parts[1].Length).IsEqualTo(32);
        await Assert.That(parts[1].All(static c => Uri.IsHexDigit(c))).IsTrue();
        await Assert.That(parts[2].Length).IsEqualTo(16);
        await Assert.That(parts[2].All(static c => Uri.IsHexDigit(c))).IsTrue();
        await Assert.That(parts[3] is "00" or "01").IsTrue();
    }

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

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }
}
