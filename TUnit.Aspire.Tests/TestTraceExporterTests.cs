using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TUnit.Aspire.Telemetry;
using TUnit.Aspire.Tests.Helpers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

// Tests use unique per-test ActivitySource names + locally-owned TracerProviders so they
// can run in parallel without env-var or static-state interference.
public class TestTraceExporterTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("not-a-uri")]
    public async Task TryParseDashboardEndpoint_InvalidValue_ReturnsNull(string? value)
    {
        await Assert.That(TestTraceExporter.TryParseDashboardEndpoint(value)).IsNull();
    }

    [Test]
    public async Task TryParseDashboardEndpoint_ValidUri_ReturnsParsed()
    {
        await Assert.That(TestTraceExporter.TryParseDashboardEndpoint("http://127.0.0.1:4317"))
            .IsEqualTo(new Uri("http://127.0.0.1:4317"));
    }

    [Test]
    public async Task AddToBuilder_ExportsTracesForRegisteredSource()
    {
        await using var server = new OtlpTraceCaptureServer();
        server.Start();

        var sourceName = $"TUnit.Tests.{Guid.NewGuid():N}";
        var endpoint = new Uri($"http://127.0.0.1:{server.Port}");

        var builder = Sdk.CreateTracerProviderBuilder().AddSource(sourceName);
        TestTraceExporter.AddToBuilder(builder, GetCurrentSessionContext(), endpoint);

        using (var activitySource = new ActivitySource(sourceName))
        using (var provider = builder.Build())
        {
            using var testCase = activitySource.StartActivity("add-to-builder case", ActivityKind.Internal);
            testCase?.Stop();
        }

        var request = await server.WaitForRequestAsync("/v1/traces");
        var body = Encoding.UTF8.GetString(request.Body);

        await Assert.That(body).Contains("add-to-builder case");
    }

    [Test]
    public async Task CreateTracerProvider_ExportsTracesForRegisteredSource()
    {
        await using var server = new OtlpTraceCaptureServer();
        server.Start();

        // Per-test ActivitySource name keeps spans isolated from any other test or production
        // listener, so this test stays parallel-safe even though OpenTelemetry exporters are
        // process-wide.
        var sourceName = $"TUnit.Tests.{Guid.NewGuid():N}";
        var endpoint = new Uri($"http://127.0.0.1:{server.Port}");

        using (var activitySource = new ActivitySource(sourceName))
        using (var provider = TestTraceExporter.CreateTracerProvider(
            endpoint, GetCurrentSessionContext(), sourceName))
        {
            using var testCase = activitySource.StartActivity("test case", ActivityKind.Internal);
            using var testBody = activitySource.StartActivity("test body", ActivityKind.Internal);

            await Assert.That(testCase).IsNotNull();
            await Assert.That(testBody).IsNotNull();

            testBody?.Stop();
            testCase?.Stop();
        }
        // Disposing the provider flushes pending spans to the exporter.

        var request = await server.WaitForRequestAsync("/v1/traces");
        var body = Encoding.UTF8.GetString(request.Body);

        await Assert.That(body).Contains("test case");
        await Assert.That(body).Contains("test body");
        await Assert.That(body).Contains(typeof(TestTraceExporterTests).Assembly.GetName().Name!);
    }

    [Test]
    public async Task GetTracesEndpoint_AppendsSignalPathWithoutDroppingBasePath()
    {
        var endpoint = new Uri("http://127.0.0.1:5341/ingest/otlp");

        var tracesEndpoint = TestTraceExporter.GetTracesEndpoint(endpoint);

        await Assert.That(tracesEndpoint.ToString()).IsEqualTo("http://127.0.0.1:5341/ingest/otlp/v1/traces");
    }

    private static TestSessionContext GetCurrentSessionContext()
    {
        return TestContext.Current!.ClassContext.AssemblyContext.TestSessionContext;
    }
}
