using System.Net;
using TUnit.AspNetCore;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Tests correlated logging via both resolution paths:
/// AsyncLocal inherited from test thread (TestServer), and
/// MakeCurrent() set by middleware when AsyncLocal is absent (simulated Kestrel).
/// </summary>
public class CorrelatedLoggingResolverTests
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TestWebAppFactory Factory { get; set; } = null!;

    [Test]
    public async Task InheritedAsyncLocal_ServerLog_CorrelatedToCorrectTest()
    {
        var marker = Guid.NewGuid().ToString("N");
        using var client = Factory.CreateClient();

        var response = await client.GetAsync($"/log/{marker}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker}");
    }

    [Test]
    public async Task InheritedAsyncLocal_MultipleRequests_EachCorrelatedToSameTest()
    {
        var marker1 = $"first_{Guid.NewGuid():N}";
        var marker2 = $"second_{Guid.NewGuid():N}";
        using var client = Factory.CreateClient();

        await client.GetAsync($"/log/{marker1}");
        await client.GetAsync($"/log/{marker2}");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker1}");
        await Assert.That(output).Contains($"SERVER_LOG:{marker2}");
    }

    [Test]
    public async Task MiddlewareMakeCurrent_ServerLog_CorrelatedToCorrectTest()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        var response = await SendWithSuppressedFlow(Factory, $"/log/{marker}", testContext);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker}");
    }

    [Test]
    public async Task MiddlewareMakeCurrent_MultipleRequests_EachCorrelatedToSameTest()
    {
        var testContext = TestContext.Current!;
        var marker1 = $"first_{Guid.NewGuid():N}";
        var marker2 = $"second_{Guid.NewGuid():N}";

        await SendWithSuppressedFlow(Factory, $"/log/{marker1}", testContext);
        await SendWithSuppressedFlow(Factory, $"/log/{marker2}", testContext);

        var output = testContext.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker1}");
        await Assert.That(output).Contains($"SERVER_LOG:{marker2}");
    }

    [Test]
    public async Task PingEndpoint_Works_WithSharedFactory()
    {
        using var client = Factory.CreateDefaultClient();
        var response = await client.GetStringAsync("/ping");
        await Assert.That(response).IsEqualTo("pong");
    }

    /// <summary>
    /// Sends an HTTP request on a thread pool thread whose execution context does NOT
    /// inherit the test's AsyncLocal values. This simulates real Kestrel behavior where
    /// the middleware's <see cref="TestContext.MakeCurrent"/> call is the only way
    /// the test context reaches server-side code.
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithSuppressedFlow(
        TestWebAppFactory factory, string path, TestContext testContext)
    {
        using var client = factory.CreateDefaultClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(TUnitTestIdHandler.HeaderName, testContext.Id);

        // SuppressFlow + Undo must run on the same thread (before the await),
        // so we capture the task first, then undo, then await.
        var flowControl = ExecutionContext.SuppressFlow();
        Task<HttpResponseMessage> task;
        try
        {
            task = Task.Run(() => client.SendAsync(request));
        }
        finally
        {
            flowControl.Undo();
        }

        return await task;
    }
}
