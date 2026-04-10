using System.Net;
using TUnit.Core;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Integration tests that verify <see cref="ITestContextResolver"/> correctly routes server-side
/// log output to the originating test when the factory is shared (PerTestSession) and the
/// server processes requests on its own thread pool threads (no AsyncLocal inheritance).
///
/// The flow being tested:
/// 1. Test sends HTTP request → TUnitTestIdHandler adds X-TUnit-TestId header
/// 2. Server receives request → TUnitTestContextMiddleware extracts test ID, stores in HttpContext.Items
/// 3. Endpoint calls ILogger.LogInformation(...)
/// 4. CorrelatedTUnitLogger resolves test context via TestContextResolverRegistry
///    → HttpContextTestContextResolver finds it from HttpContext.Items
/// 5. Console interceptor routes output to the correct test's output buffer
/// 6. Test verifies its own GetStandardOutput() contains the expected marker
/// </summary>
[NotInParallel("CorrelatedLogging")]
public class CorrelatedLoggingResolverTests
{
    /// <summary>
    /// Shared factory (PerTestSession) — no per-test TestContext is injected into the DI container.
    /// The resolver mechanism is the only way server-side logs get correlated.
    /// </summary>
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public TestWebAppFactory Factory { get; set; } = null!;

    [Test]
    public async Task ServerLog_CorrelatedToCorrectTest_ViaResolver()
    {
        // Arrange - unique marker for this test instance
        var marker = Guid.NewGuid().ToString("N");

        // Create client with TUnitTestIdHandler to propagate test context
        var client = Factory.CreateDefaultClient(new TUnitTestIdHandler());

        // Act - hit the logging endpoint; the server logs "SERVER_LOG:{marker}"
        var response = await client.GetAsync($"/log/{marker}");

        // Assert - request succeeded
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Assert - the server-side log message was routed to THIS test's output
        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker}");
    }

    [Test]
    public async Task PingEndpoint_Works_WithSharedFactory()
    {
        // Verify the shared factory serves requests correctly
        var client = Factory.CreateDefaultClient(new TUnitTestIdHandler());

        var response = await client.GetStringAsync("/ping");

        await Assert.That(response).IsEqualTo("pong");
    }

    [Test]
    public async Task MultipleRequests_EachCorrelatedToSameTest()
    {
        var marker1 = $"first_{Guid.NewGuid():N}";
        var marker2 = $"second_{Guid.NewGuid():N}";

        var client = Factory.CreateDefaultClient(new TUnitTestIdHandler());

        // Send two requests from the same test
        await client.GetAsync($"/log/{marker1}");
        await client.GetAsync($"/log/{marker2}");

        // Both markers should appear in this test's output
        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"SERVER_LOG:{marker1}");
        await Assert.That(output).Contains($"SERVER_LOG:{marker2}");
    }
}
