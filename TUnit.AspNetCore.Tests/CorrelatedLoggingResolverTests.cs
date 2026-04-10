using System.Net;
using TUnit.AspNetCore;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Integration tests verifying correlated logging works via both resolution paths:
/// <list type="bullet">
///   <item><b>AsyncLocal path</b> — TestServer inherits the test's execution context,
///     so <see cref="TestContext.Current"/> is available server-side (fast path).</item>
///   <item><b>Resolver path</b> — <see cref="ExecutionContext.SuppressFlow"/> breaks AsyncLocal
///     inheritance (simulating real Kestrel), forcing fallback to
///     <see cref="TestContextResolverRegistry"/> → <c>HttpContextTestContextResolver</c>
///     → <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/>.</item>
/// </list>
/// </summary>
public class CorrelatedLoggingResolverTests
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TestWebAppFactory Factory { get; set; } = null!;

    // ── AsyncLocal path (TestServer inherits execution context) ──

    [Test]
    public async Task AsyncLocalPath_ServerLog_CorrelatedToCorrectTest()
    {
        var marker = Guid.NewGuid().ToString("N");
        var client = Factory.CreateDefaultClient(new TUnitTestIdHandler());

        var response = await client.GetAsync($"/log/{marker}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker}");
    }

    [Test]
    public async Task AsyncLocalPath_MultipleRequests_EachCorrelatedToSameTest()
    {
        var marker1 = $"first_{Guid.NewGuid():N}";
        var marker2 = $"second_{Guid.NewGuid():N}";
        var client = Factory.CreateDefaultClient(new TUnitTestIdHandler());

        await client.GetAsync($"/log/{marker1}");
        await client.GetAsync($"/log/{marker2}");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker1}");
        await Assert.That(output).Contains($"SERVER_LOG:{marker2}");
    }

    // ── Resolver path (AsyncLocal suppressed to simulate real Kestrel) ──

    [Test]
    public async Task ResolverPath_ServerLog_CorrelatedToCorrectTest()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        var response = await SendWithSuppressedFlow(
            Factory, $"/log/{marker}", testContext.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker}");
    }

    [Test]
    public async Task ResolverPath_MultipleRequests_EachCorrelatedToSameTest()
    {
        var testContext = TestContext.Current!;
        var marker1 = $"first_{Guid.NewGuid():N}";
        var marker2 = $"second_{Guid.NewGuid():N}";

        await SendWithSuppressedFlow(Factory, $"/log/{marker1}", testContext.Id);
        await SendWithSuppressedFlow(Factory, $"/log/{marker2}", testContext.Id);

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker1}");
        await Assert.That(output).Contains($"SERVER_LOG:{marker2}");
    }

    // ── Basic sanity ──

    [Test]
    public async Task PingEndpoint_Works_WithSharedFactory()
    {
        var client = Factory.CreateDefaultClient();
        var response = await client.GetStringAsync("/ping");
        await Assert.That(response).IsEqualTo("pong");
    }

    /// <summary>
    /// Sends an HTTP request with the test ID header on a thread pool thread
    /// whose execution context does NOT inherit the test's AsyncLocal values.
    /// This simulates real Kestrel behavior where server threads are independent
    /// of the test's async flow, forcing the resolver fallback path.
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithSuppressedFlow(
        TestWebAppFactory factory, string path, string testId)
    {
        var client = factory.CreateDefaultClient();
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(TUnitTestIdHandler.HeaderName, testId);

        // Suppress flow, start the task (queued WITHOUT execution context), then
        // restore flow immediately on the same thread to avoid cross-thread Undo().
        var flowControl = ExecutionContext.SuppressFlow();
        var task = Task.Run(() => client.SendAsync(request));
        flowControl.Undo();

        return await task;
    }
}
