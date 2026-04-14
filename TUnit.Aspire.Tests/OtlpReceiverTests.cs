using System.Diagnostics;
using System.Net;
using TUnit.Aspire.Telemetry;
using TUnit.Aspire.Tests.Helpers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class OtlpReceiverTests
{
    [Test]
    public async Task Receiver_ListensOnDynamicPort()
    {
        await using var receiver = new OtlpReceiver();

        await Assert.That(receiver.Port).IsGreaterThan(0);
    }

    [Test]
    public async Task Receiver_AcceptsPostToV1Logs_Returns200()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();
        var content = new ByteArrayContent([]);
        content.Headers.ContentType = new("application/x-protobuf");

        var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Receiver_AcceptsPostToV1Traces_Returns200()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();
        var content = new ByteArrayContent([]);
        content.Headers.ContentType = new("application/x-protobuf");

        var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Receiver_RejectsGetRequest_Returns405()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();
        var response = await client.GetAsync($"http://127.0.0.1:{receiver.Port}/v1/logs");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task Receiver_CorrelatesLogs_ToCorrectTestOutput()
    {
        // Arrange: get the current test context and register a trace ID for it
        var testContext = TestContext.Current!;
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "test-node-correlate", testContext.Id);

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        // Act: POST an OTLP log message with the registered trace ID
        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "my-api",
            new LogRecordSpec
            {
                TraceId = traceId,
                SeverityNumber = 9,
                SeverityText = "INFO",
                Body = "Hello from the SUT!",
            });

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Assert: the log should appear in this test's output
        var output = await PollForOutput(testContext, "Hello from the SUT!");
        await Assert.That(output).Contains("[my-api]");
        await Assert.That(output).Contains("[INFO]");
    }

    [Test]
    public async Task Receiver_CorrelatesLogs_MultipleSeverityLevels()
    {
        var testContext = TestContext.Current!;
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "test-node-severity", testContext.Id);

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "worker",
            new LogRecordSpec { TraceId = traceId, SeverityNumber = 5, Body = "Debug message" },
            new LogRecordSpec { TraceId = traceId, SeverityNumber = 9, Body = "Info message" },
            new LogRecordSpec { TraceId = traceId, SeverityNumber = 17, Body = "Error message" });

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        var output = await PollForOutput(testContext, "Error message");
        await Assert.That(output).Contains("[DEBUG] Debug message");
        await Assert.That(output).Contains("[INFO] Info message");
        await Assert.That(output).Contains("[ERROR] Error message");
    }

    [Test]
    public async Task Receiver_IgnoresLogs_WithUnknownTraceId()
    {
        var testContext = TestContext.Current!;
        var unknownTraceId = Guid.NewGuid().ToString("N");

        // Deliberately do NOT register this trace ID
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "rogue-service",
            new LogRecordSpec
            {
                TraceId = unknownTraceId,
                SeverityNumber = 9,
                Body = "This should not appear in test output",
            });

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        // No matching trace ID — give a short window then verify nothing appeared
        await Task.Delay(200);
        var output = testContext.GetStandardOutput();
        await Assert.That(output).DoesNotContain("This should not appear in test output");
    }

    [Test]
    public async Task Receiver_CorrelatesLogs_FromMultipleResources_ToSameTest()
    {
        var testContext = TestContext.Current!;
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "test-node-multi-resource", testContext.Id);

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        // Two different services emit logs with the same trace ID (propagated via HTTP)
        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            ("api-gateway", [new LogRecordSpec { TraceId = traceId, SeverityNumber = 9, Body = "Gateway received request" }]),
            ("backend-api", [new LogRecordSpec { TraceId = traceId, SeverityNumber = 9, Body = "Backend processing request" }]));

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        var output = await PollForOutput(testContext, "Backend processing request");
        await Assert.That(output).Contains("[api-gateway]");
        await Assert.That(output).Contains("Gateway received request");
        await Assert.That(output).Contains("[backend-api]");
    }

    [Test]
    public async Task Receiver_CorrelatesLogs_ConcurrentTests_NoBleedBetweenTests()
    {
        // This test verifies that logs with different trace IDs are routed
        // to the correct test context, not to unrelated tests.
        var testContext = TestContext.Current!;
        var myTraceId = Guid.NewGuid().ToString("N");
        var otherTraceId = Guid.NewGuid().ToString("N");
        var otherContextId = Guid.NewGuid().ToString();

        // Register my trace ID for this test
        TraceRegistry.Register(myTraceId, "test-node-mine", testContext.Id);
        // Register another trace ID for a fake "other test"
        // (contextId doesn't match any real TestContext, so GetById returns null)
        TraceRegistry.Register(otherTraceId, "test-node-other", otherContextId);

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "shared-svc",
            new LogRecordSpec { TraceId = myTraceId, SeverityNumber = 9, Body = "My log" },
            new LogRecordSpec { TraceId = otherTraceId, SeverityNumber = 9, Body = "Other test's log" });

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        var output = await PollForOutput(testContext, "My log");
        // The other test's log should NOT appear in this test's output
        // (it targets a non-existent TestContext, so it's silently dropped)
        await Assert.That(output).DoesNotContain("Other test's log");
    }

    [Test]
    public async Task Receiver_LogWithNoResourceName_FormatsWithoutPrefix()
    {
        var testContext = TestContext.Current!;
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "test-node-no-resource", testContext.Id);

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            serviceName: null,
            new LogRecordSpec
            {
                TraceId = traceId,
                SeverityNumber = 13,
                Body = "Warning without service name",
            });

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        // Should have severity but no resource prefix
        var output = await PollForOutput(testContext, "Warning without service name");
        await Assert.That(output).Contains("[WARN] Warning without service name");
    }

    [Test]
    public async Task Receiver_ForwardsToUpstream_WhenConfigured()
    {
        // Start a second receiver to act as the "upstream" (e.g., Aspire dashboard)
        await using var upstream = new OtlpReceiver();
        upstream.Start();

        // Start the main receiver with forwarding configured
        await using var receiver = new OtlpReceiver($"http://127.0.0.1:{upstream.Port}");
        receiver.Start();

        var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
            "forwarded-svc",
            new LogRecordSpec
            {
                TraceId = Guid.NewGuid().ToString("N"),
                SeverityNumber = 9,
                Body = "This should be forwarded",
            });

        using var client = new HttpClient();
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");

        var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Allow forwarding to complete
        await Task.Delay(500);

        // We can't easily check the upstream received it without more infrastructure,
        // but we verify the main receiver doesn't fail when forwarding is configured.
    }

    [Test]
    public async Task Receiver_HandlesMultipleSequentialRequests()
    {
        var testContext = TestContext.Current!;
        var traceId = Guid.NewGuid().ToString("N");

        TraceRegistry.Register(traceId, "test-node-sequential", testContext.Id);

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();

        for (var i = 0; i < 5; i++)
        {
            var body = OtlpProtobufBuilder.BuildExportLogsServiceRequest(
                "api",
                new LogRecordSpec { TraceId = traceId, SeverityNumber = 9, Body = $"Request {i}" });

            var content = new ByteArrayContent(body);
            content.Headers.ContentType = new("application/x-protobuf");

            var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        var output = await PollForOutput(testContext, "Request 4");
        for (var i = 0; i < 5; i++)
        {
            await Assert.That(output).Contains($"Request {i}");
        }
    }

    [Test]
    public async Task Receiver_MalformedProtobuf_Returns200_SilentlySkips()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        // Send garbage data
        using var client = new HttpClient();
        var content = new ByteArrayContent([0xFF, 0xFF, 0xFF, 0xFF]);
        content.Headers.ContentType = new("application/x-protobuf");

        var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", content);

        // Should still return 200 (malformed protobuf is silently skipped)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>
    /// Polls test output until it contains the expected text or a timeout is reached.
    /// Replaces fixed <c>Task.Delay</c> waits to avoid flaky timing on slow CI agents.
    /// </summary>
    private static async Task<string> PollForOutput(TestContext testContext, string expected, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var output = testContext.GetStandardOutput();
            if (output.Contains(expected))
            {
                return output;
            }

            await Task.Delay(50);
        }

        return testContext.GetStandardOutput();
    }
}
