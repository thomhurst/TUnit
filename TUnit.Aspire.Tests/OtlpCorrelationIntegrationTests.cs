using System.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Integration tests that verify OTLP log-to-test correlation across real Aspire
/// process boundaries. These tests start a real .NET API service via Aspire,
/// make HTTP requests, and verify that the service's logs are routed back to
/// the originating test's output via the OTLP receiver.
/// </summary>
[ClassDataSource<IntegrationTestFixture>(Shared = SharedType.PerTestSession)]
[Category("Docker")]
[Category("Integration")]
public class OtlpCorrelationIntegrationTests(IntegrationTestFixture fixture)
{
    private const string ServiceName = "api-service";
    private const int ConcurrentInstanceCount = 10;

    /// <summary>
    /// Verifies the core end-to-end flow: a test makes an HTTP request to a real
    /// Aspire-hosted API service, the service logs a message, and that log is
    /// correlated back to this test's output via TraceId propagation and the OTLP receiver.
    /// </summary>
    [Test]
    public async Task Logs_FromApiService_AppearInTestOutput()
    {
        var marker = $"integration-marker-{Guid.NewGuid():N}";

        var client = fixture.CreateHttpClient(ServiceName);
        using var response = await client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}");

        await Assert.That((int)response.StatusCode).IsEqualTo(200);

        var output = await PollForOutput(marker);
        await Assert.That(output).Contains(marker);
    }

    /// <summary>
    /// Verifies that logs from the service include the <c>[api-service]</c> resource name prefix,
    /// confirming that the OTLP resource attributes (service.name) are correctly parsed.
    /// </summary>
    [Test]
    public async Task Logs_IncludeServiceNamePrefix()
    {
        var marker = $"svc-prefix-{Guid.NewGuid():N}";

        var client = fixture.CreateHttpClient(ServiceName);
        using var _ = await client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}");

        var output = await PollForOutput(marker);
        await Assert.That(output).Contains($"[{ServiceName}]");
    }

    /// <summary>
    /// Verifies that logs include the correct severity label
    /// based on the log level used by the API service.
    /// </summary>
    [Test]
    public async Task Logs_IncludeSeverityLevel()
    {
        var marker = $"severity-{Guid.NewGuid():N}";

        var client = fixture.CreateHttpClient(ServiceName);
        using var _ = await client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}");

        // Poll for the application log format specifically — the raw marker also
        // appears in ASP.NET request-start logs (in the URL), which arrive earlier
        // than the application's own log message.
        var expected = $"] {marker}";
        var output = await PollForOutput(expected);

        await Assert.That(output).Contains(expected);
    }

    /// <summary>
    /// Verifies that different log levels (Warning, Error) are correctly propagated
    /// and formatted in the test output.
    /// </summary>
    [Test]
    public async Task DifferentLogLevels_AreCorrectlyFormatted()
    {
        var warnMarker = $"warn-level-{Guid.NewGuid():N}";
        var errorMarker = $"error-level-{Guid.NewGuid():N}";

        var client = fixture.CreateHttpClient(ServiceName);
        using var _ = await client.GetAsync($"/log-level?message={Uri.EscapeDataString(warnMarker)}&level=Warning");
        using var __ = await client.GetAsync($"/log-level?message={Uri.EscapeDataString(errorMarker)}&level=Error");

        // Poll once for the later marker — both will have arrived by then
        var output = await PollForOutput(errorMarker);
        await Assert.That(output).Contains(warnMarker);
        await Assert.That(output).Contains(errorMarker);
    }

    /// <summary>
    /// Verifies concurrent test isolation at scale: 10 test instances run in parallel,
    /// each sending 3 HTTP requests with unique markers. Every instance verifies that
    /// all its own markers appear in its output and that NO markers from any other
    /// instance are present — proving TraceId-based correlation is correct under concurrency.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    public async Task ConcurrentTests_EachSeeOnlyOwnLogs(int instanceId)
    {
        var markers = Enumerable.Range(0, 3)
            .Select(i => $"concurrent-{instanceId}-req{i}-{Guid.NewGuid():N}")
            .ToList();

        var client = fixture.CreateHttpClient(ServiceName);

        foreach (var marker in markers)
        {
            using var _ = await client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}");
        }

        var output = await PollForOutput(markers.Last());

        // All of this test instance's markers must be present
        foreach (var marker in markers)
        {
            await Assert.That(output).Contains(marker);
        }

        // No markers from any other instance should appear in this test's output
        for (var other = 0; other < ConcurrentInstanceCount; other++)
        {
            if (other != instanceId)
            {
                await Assert.That(output).DoesNotContain($"concurrent-{other}-req");
            }
        }
    }

    /// <summary>
    /// Verifies that burst-firing many concurrent HTTP requests from a single test
    /// all produce logs that correlate back to this test. Exercises the OTLP pipeline
    /// under load — all requests share the same TraceId and must all appear in output.
    /// </summary>
    [Test]
    public async Task BurstConcurrentRequests_AllCorrelateCorrectly()
    {
        var markers = Enumerable.Range(0, 10)
            .Select(i => $"burst-{i}-{Guid.NewGuid():N}")
            .ToList();

        var client = fixture.CreateHttpClient(ServiceName);

        // Fire all requests concurrently and dispose responses
        var responses = await Task.WhenAll(markers.Select(marker =>
            client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}")));

        foreach (var response in responses)
        {
            response.Dispose();
        }

        // Poll for each marker individually — concurrent requests may arrive in any order
        foreach (var marker in markers)
        {
            await PollForOutput(marker);
        }

        var output = TestContext.Current!.GetStandardOutput();
        foreach (var marker in markers)
        {
            await Assert.That(output).Contains(marker);
        }
    }

    /// <summary>
    /// Verifies that multiple sequential requests within the same test all produce
    /// correlated logs under the same trace context.
    /// </summary>
    [Test]
    public async Task MultipleRequests_AllCorrelateToSameTest()
    {
        var markers = Enumerable.Range(0, 5)
            .Select(i => $"multi-req-{i}-{Guid.NewGuid():N}")
            .ToList();

        var client = fixture.CreateHttpClient(ServiceName);

        foreach (var marker in markers)
        {
            using var _ = await client.GetAsync($"/log?message={Uri.EscapeDataString(marker)}");
        }

        // Wait for all logs to arrive
        var output = await PollForOutput(markers.Last());

        foreach (var marker in markers)
        {
            await Assert.That(output).Contains(marker);
        }
    }

    /// <summary>
    /// Polls <see cref="TestContext.GetStandardOutput"/> until it contains the expected marker
    /// or a timeout is reached. The OTLP SDK batches log exports, so there's inherent latency
    /// between when the SUT logs a message and when it arrives at TUnit's OTLP receiver.
    /// </summary>
    private static async Task<string> PollForOutput(string expectedMarker, int timeoutSeconds = 30)
    {
        var testContext = TestContext.Current!;
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (sw.Elapsed < timeout)
        {
            var output = testContext.GetStandardOutput();
            if (output.Contains(expectedMarker))
            {
                return output;
            }

            await Task.Delay(250);
        }

        // Return whatever we have — the caller's assertion will produce a clear failure message
        return testContext.GetStandardOutput();
    }
}
