using System.Net;
using TUnit.AspNetCore;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Tests both AsyncLocal and resolver-based test context correlation paths.
/// </summary>
public class CorrelatedLoggingResolverTests
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TestWebAppFactory Factory { get; set; } = null!;

    [Test]
    public async Task AsyncLocalPath_ServerLog_CorrelatedToCorrectTest()
    {
        var marker = Guid.NewGuid().ToString("N");
        using var client = Factory.CreateClientWithTestContext();

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
        using var client = Factory.CreateClientWithTestContext();

        await client.GetAsync($"/log/{marker1}");
        await client.GetAsync($"/log/{marker2}");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker1}");
        await Assert.That(output).Contains($"SERVER_LOG:{marker2}");
    }

    [Test]
    public async Task ResolverPath_ServerLog_CorrelatedToCorrectTest()
    {
        var testContext = TestContext.Current!;
        var marker = Guid.NewGuid().ToString("N");

        var response = await SendWithSuppressedFlow(Factory, $"/log/{marker}", testContext);

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
    /// inherit the test's AsyncLocal values, simulating real Kestrel behavior and forcing
    /// the resolver fallback path.
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithSuppressedFlow(
        TestWebAppFactory factory, string path, TestContext testContext)
    {
        using var client = factory.CreateDefaultClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(TUnitTestIdHandler.HeaderName, testContext.Id);

        var flowControl = ExecutionContext.SuppressFlow();
        var task = Task.Run(() => client.SendAsync(request));
        flowControl.Undo();

        return await task;
    }
}
