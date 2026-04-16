using System.Diagnostics;
using System.Text;
using TUnit.Aspire.Telemetry;
using TUnit.Aspire.Tests.Helpers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class TestTraceExporterTests
{
    private const string DashboardOtlpEndpointEnvVar = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";

    [Test]
    public async Task TryStart_DashboardEndpointMissing_DoesNotStartExporter()
    {
        var previousEndpoint = Environment.GetEnvironmentVariable(DashboardOtlpEndpointEnvVar);
        Environment.SetEnvironmentVariable(DashboardOtlpEndpointEnvVar, null);
        TestTraceExporter.Dispose();

        try
        {
            TestTraceExporter.TryStart(GetCurrentSessionContext());

            await Assert.That(TestTraceExporter.IsStarted).IsFalse();
        }
        finally
        {
            TestTraceExporter.Dispose();
            Environment.SetEnvironmentVariable(DashboardOtlpEndpointEnvVar, previousEndpoint);
        }
    }

    [Test]
    public async Task TryStart_ConfiguredDashboardEndpoint_ExportsTestTrace()
    {
        var previousEndpoint = Environment.GetEnvironmentVariable(DashboardOtlpEndpointEnvVar);
        await using var server = new OtlpTraceCaptureServer();
        server.Start();

        Environment.SetEnvironmentVariable(DashboardOtlpEndpointEnvVar, $"http://127.0.0.1:{server.Port}");
        TestTraceExporter.Dispose();

        try
        {
            TestTraceExporter.TryStart(GetCurrentSessionContext());

            await Assert.That(TestTraceExporter.IsStarted).IsTrue();

            using var activitySource = new ActivitySource("TUnit");
            using var testCase = activitySource.StartActivity("test case", ActivityKind.Internal);
            using var testBody = activitySource.StartActivity("test body", ActivityKind.Internal);

            await Assert.That(testCase).IsNotNull();
            await Assert.That(testBody).IsNotNull();

            testBody?.Stop();
            testCase?.Stop();
            TestTraceExporter.Dispose();

            var request = await server.WaitForRequestAsync("/v1/traces");
            var body = request.Body;

            await Assert.That(Encoding.UTF8.GetString(body)).Contains("test case");
            await Assert.That(Encoding.UTF8.GetString(body)).Contains("test body");
            await Assert.That(Encoding.UTF8.GetString(body)).Contains(typeof(TestTraceExporterTests).Assembly.GetName().Name!);
        }
        finally
        {
            TestTraceExporter.Dispose();
            Environment.SetEnvironmentVariable(DashboardOtlpEndpointEnvVar, previousEndpoint);
        }
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
