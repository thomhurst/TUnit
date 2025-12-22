using System.Net;
using System.Text;
using TUnit.AspNetCore;
using TUnit.AspNetCore.Interception;

namespace TUnit.AspNetCore.NugetTester;

/// <summary>
/// Tests verifying that HTTP exchange capture works correctly when TUnit.AspNetCore
/// is consumed as a NuGet package.
/// </summary>
public class HttpExchangeCaptureTests : TestsBase
{
    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        // Enable HTTP exchange capture for these tests
        options.EnableHttpExchangeCapture = true;
    }

    /// <summary>
    /// Gets the HttpExchangeCapture from DI services.
    /// Note: Use this instead of the HttpCapture property until bug is fixed.
    /// </summary>
    private HttpExchangeCapture Capture => Services.GetRequiredService<HttpExchangeCapture>();

    [Test]
    public async Task HttpCapture_IsAvailableInServices_WhenEnabled()
    {
        // Verifies that HttpExchangeCapture is registered when enabled in options
        var capture = Services.GetService<HttpExchangeCapture>();
        await Assert.That(capture).IsNotNull();
    }

    [Test]
    public async Task HttpCapture_CapturesGetRequest()
    {
        // Verifies that GET requests are captured
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");

        await Assert.That(Capture.Count).IsEqualTo(1);
        await Assert.That(Capture.Last!.Request.Method).IsEqualTo("GET");
        await Assert.That(Capture.Last.Request.Path).IsEqualTo("/ping");
    }

    [Test]
    public async Task HttpCapture_CapturesResponseStatusCode()
    {
        // Verifies that response status codes are captured
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");

        await Assert.That(Capture.Last!.Response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(Capture.Last.Response.StatusCodeValue).IsEqualTo(200);
    }

    [Test]
    public async Task HttpCapture_CapturesResponseBody()
    {
        // Verifies that response body is captured
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");

        await Assert.That(Capture.Last!.Response.Body).IsEqualTo("pong");
    }

    [Test]
    public async Task HttpCapture_CapturesPostRequestBody()
    {
        // Verifies that POST request body is captured
        var client = Factory.CreateClient();
        var content = new StringContent("test payload", Encoding.UTF8, "text/plain");
        await client.PostAsync("/echo", content);

        await Assert.That(Capture.Last!.Request.Method).IsEqualTo("POST");
        await Assert.That(Capture.Last.Request.Body).Contains("test payload");
    }

    [Test]
    public async Task HttpCapture_CapturesResponseHeaders()
    {
        // Verifies that response headers are captured
        var client = Factory.CreateClient();
        await client.GetAsync("/status");

        await Assert.That(Capture.Last!.Response.Headers)
            .ContainsKey("X-Custom-Header");
    }

    [Test]
    public async Task HttpCapture_CapturesMultipleExchanges()
    {
        // Verifies that multiple exchanges are captured in order
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");
        await client.GetAsync("/status");
        await client.GetAsync("/greet/World");

        await Assert.That(Capture.Count).IsEqualTo(3);
        await Assert.That(Capture.First!.Request.Path).IsEqualTo("/ping");
        await Assert.That(Capture.Last!.Request.Path).IsEqualTo("/greet/World");
    }

    [Test]
    public async Task HttpCapture_ForMethod_FiltersCorrectly()
    {
        // Verifies that filtering by HTTP method works
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");
        await client.PostAsync("/echo", new StringContent("test"));

        var getExchanges = Capture.ForMethod("GET");
        var postExchanges = Capture.ForMethod("POST");

        await Assert.That(getExchanges.Count()).IsEqualTo(1);
        await Assert.That(postExchanges.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task HttpCapture_ForPath_FiltersCorrectly()
    {
        // Verifies that filtering by path works
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");
        await client.GetAsync("/status");

        var pingExchanges = Capture.ForPath("/ping");

        await Assert.That(pingExchanges.Count()).IsEqualTo(1);
        await Assert.That(pingExchanges.First()!.Request.Path).IsEqualTo("/ping");
    }

    [Test]
    public async Task HttpCapture_ForPathStartingWith_FiltersCorrectly()
    {
        // Verifies that filtering by path prefix works
        var client = Factory.CreateClient();
        await client.GetAsync("/greet/Alice");
        await client.GetAsync("/greet/Bob");
        await client.GetAsync("/ping");

        var greetExchanges = Capture.ForPathStartingWith("/greet");

        await Assert.That(greetExchanges.Count()).IsEqualTo(2);
    }

    [Test]
    public async Task HttpCapture_Clear_RemovesAllExchanges()
    {
        // Verifies that Clear() works correctly
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");
        await client.GetAsync("/status");

        await Assert.That(Capture.Count).IsEqualTo(2);

        Capture.Clear();

        await Assert.That(Capture.Count).IsEqualTo(0);
    }

    [Test]
    public async Task HttpCapture_TracksDuration()
    {
        // Verifies that request duration is tracked
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");

        await Assert.That(Capture.Last!.Duration).IsGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task HttpCapture_TracksTimestamp()
    {
        // Verifies that timestamp is tracked
        var before = DateTime.UtcNow;
        var client = Factory.CreateClient();
        await client.GetAsync("/ping");
        var after = DateTime.UtcNow;

        await Assert.That(Capture.Last!.Timestamp).IsGreaterThanOrEqualTo(before);
        await Assert.That(Capture.Last.Timestamp).IsLessThanOrEqualTo(after);
    }
}
