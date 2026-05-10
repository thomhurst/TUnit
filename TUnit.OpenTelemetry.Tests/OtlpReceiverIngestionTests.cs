using System.Diagnostics;
using System.Net;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Engine.Reporters.Html;
using TUnit.OpenTelemetry.Receiver;

namespace TUnit.OpenTelemetry.Tests;

public class OtlpReceiverIngestionTests
{
    private const ulong SpanKindConsumer = 5; // OTLP SpanKind.CONSUMER
    private const string TestSpanId = "0123456789abcdef";
    private const ulong TestStartTimeUnixNano = 1;
    private const ulong TestEndTimeUnixNano = 2;

    [Test]
    public async Task Receiver_ParsedTrace_ReachesActivityCollector()
    {
        var collector = ActivityCollector.Current;
        await Assert.That(collector).IsNotNull();

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        var sourceName = $"TUnit.ReceiverIngestionTest.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri($"http://127.0.0.1:{receiver.Port}/v1/traces");
                o.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();

        string traceId;
        using (var activity = source.StartActivity("sut-external-op"))
        {
            await Assert.That(activity).IsNotNull();
            traceId = activity!.TraceId.ToString();
            collector!.RegisterExternalTrace(traceId);
        }

        provider!.ForceFlush(5000);
        await receiver.WhenIdle();

        var span = collector!.GetAllSpans().FirstOrDefault(s =>
            s.TraceId == traceId && s.Name == "sut-external-op");

        await Assert.That(span).IsNotNull();
    }

    [Test]
    public async Task Receiver_Diagnostics_ClassifiesEachRequestPath()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();
        using var emptyTraces = new ByteArrayContent(Array.Empty<byte>());
        using var emptyLogs = new ByteArrayContent(Array.Empty<byte>());
        using var emptyMetrics = new ByteArrayContent(Array.Empty<byte>());
        using var emptyOther = new ByteArrayContent(Array.Empty<byte>());

        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", emptyTraces);
        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/logs", emptyLogs);
        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/metrics", emptyMetrics);
        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/some/other/path", emptyOther);

        await receiver.WhenIdle();

        var diag = receiver.Diagnostics;
        await Assert.That(diag.TracesRequests).IsEqualTo(1);
        await Assert.That(diag.LogsRequests).IsEqualTo(1);
        await Assert.That(diag.MetricsRequests).IsEqualTo(1);
        await Assert.That(diag.OtherRequests).IsEqualTo(1);
        await Assert.That(diag.TotalRequests).IsEqualTo(4);

        var summary = diag.FormatSummary(receiver.Port);
        await Assert.That(summary).Contains("requests.v1_traces                   = 1");
        await Assert.That(summary).Contains("requests.v1_metrics                  = 1");
        await Assert.That(summary).Contains("other_path[/some/other/path] = 1");
    }

    [Test]
    public async Task Receiver_GrpcRequest_RejectedWith415AndCounted()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();

        using var grpcByContentType = new ByteArrayContent(Array.Empty<byte>());
        grpcByContentType.Headers.TryAddWithoutValidation("Content-Type", "application/grpc");
        var contentTypeResponse = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", grpcByContentType);

        using var grpcByPath = new ByteArrayContent(Array.Empty<byte>());
        var pathResponse = await client.PostAsync(
            $"http://127.0.0.1:{receiver.Port}/opentelemetry.proto.collector.trace.v1.TraceService/Export",
            grpcByPath);

        await receiver.WhenIdle();

        await Assert.That((int)contentTypeResponse.StatusCode).IsEqualTo(415);
        await Assert.That((int)pathResponse.StatusCode).IsEqualTo(415);
        await Assert.That(receiver.Diagnostics.GrpcRejected).IsEqualTo(2);
        await Assert.That(receiver.Diagnostics.TracesRequests).IsEqualTo(0);

        // Content-type-triggered rejection must NOT pollute the unknown-paths map with
        // /v1/traces — only the path-triggered rejection should record its path.
        var summary = receiver.Diagnostics.FormatSummary(receiver.Port);
        await Assert.That(summary).DoesNotContain("other_path[/v1/traces]");
        await Assert.That(summary).Contains("other_path[/opentelemetry.proto.collector.trace.v1.TraceService/Export]");
    }

    [Test]
    public async Task Receiver_BatchOfSameTrace_CountsRegistrationOncePerTrace()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        // Register a fake trace so all three spans hit the "already registered" path.
        var traceId = Guid.NewGuid().ToString("N");
        TraceRegistry.Register(traceId, "fake-test-context-id");

        var body = BuildMultiSpanBatch(traceId, spanCount: 3);

        using var client = new HttpClient();
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");
        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", content);

        await receiver.WhenIdle();

        await Assert.That(receiver.Diagnostics.TracesSpansParsed).IsEqualTo(3);
        await Assert.That(receiver.Diagnostics.TracesAlreadyRegistered).IsEqualTo(1);
        await Assert.That(receiver.Diagnostics.TracesNoMatch).IsEqualTo(0);
    }

    [Test]
    public async Task Receiver_Forwarding_PropagatesHeadersAndCountsSuccess()
    {
        // Mock upstream stands in for the Aspire dashboard — gates on the api-key header
        // so we can prove the receiver actually propagated it instead of just dropping body
        // bytes onto a permissive endpoint.
        using var upstreamListener = new HttpListener();
        var upstreamPort = LoopbackHttpListenerFactory.FindFreePort();
        upstreamListener.Prefixes.Add($"http://127.0.0.1:{upstreamPort}/");
        upstreamListener.Start();

        var receivedAuth = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listenerTask = Task.Run(async () =>
        {
            var ctx = await upstreamListener.GetContextAsync();
            receivedAuth.TrySetResult(ctx.Request.Headers["x-otlp-api-key"]);
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        });

        try
        {
            await using var receiver = new OtlpReceiver($"http://127.0.0.1:{upstreamPort}")
            {
                UpstreamHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["x-otlp-api-key"] = "test-token-abc",
                },
            };
            receiver.Start();

            using var client = new HttpClient();
            using var content = new ByteArrayContent(Array.Empty<byte>());
            content.Headers.ContentType = new("application/x-protobuf");
            await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", content);

            await receiver.DrainAsync(TimeSpan.FromSeconds(3));

            var auth = await receivedAuth.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await Assert.That(auth).IsEqualTo("test-token-abc");
            await Assert.That(receiver.Diagnostics.UpstreamForwardSuccess).IsEqualTo(1);
            await Assert.That(receiver.Diagnostics.UpstreamForwardFailures).IsEqualTo(0);
        }
        finally
        {
            upstreamListener.Stop();
            // Ignore — listener context may already be torn down on assertion failure.
            try { await listenerTask; } catch { }
        }
    }

    [Test]
    public async Task Receiver_DrainAsync_WaitsForLatePostBeforeReturning()
    {
        await using var receiver = new OtlpReceiver();
        receiver.Start();

        // Simulate a SUT exporter that flushes a couple hundred ms after the test logic
        // would finish — without DrainAsync, AspireFixture would tear down the AppHost
        // and the late POST would fail / be dropped.
        var latePost = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            using var client = new HttpClient();
            using var content = new ByteArrayContent(Array.Empty<byte>());
            await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", content);
        });

        var drainStart = DateTime.UtcNow;
        await receiver.DrainAsync(TimeSpan.FromSeconds(3));
        var drainElapsed = DateTime.UtcNow - drainStart;

        await latePost;

        await Assert.That(receiver.Diagnostics.TracesRequests).IsEqualTo(1);
        // The drain must have observed the late request — i.e., it didn't return at the
        // 250ms stable window before the 200ms-delayed POST landed.
        await Assert.That(drainElapsed).IsGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(400));
        // And it must respect the cap — no point waiting indefinitely once quiet.
        await Assert.That(drainElapsed).IsLessThan(TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task Receiver_ParsedLinkedTrace_RegistersAgainstOwningTest()
    {
        var collector = ActivityCollector.Current;
        await Assert.That(collector).IsNotNull();
        await Assert.That(Activity.Current).IsNotNull();

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        var linkedContext = Activity.Current!.Context;
        var derivedTraceId = Guid.NewGuid().ToString("N");
        var body = BuildLinkedTraceExportRequest(
            derivedTraceId,
            TestSpanId,
            "sut-linked-op",
            linkedContext.TraceId.ToString(),
            linkedContext.SpanId.ToString());

        using var client = new HttpClient();
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new("application/x-protobuf");
        var response = await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", content);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await receiver.WhenIdle();

        var span = collector!.GetAllSpans().FirstOrDefault(s =>
            s.TraceId == derivedTraceId && s.Name == "sut-linked-op");

        await Assert.That(span).IsNotNull();
        await Assert.That(TraceRegistry.IsRegistered(derivedTraceId)).IsTrue();
        await Assert.That(TraceRegistry.GetContextId(derivedTraceId)).IsEqualTo(TestContext.Current!.Id);
    }

    private static byte[] BuildMultiSpanBatch(string traceId, int spanCount)
    {
        // Build N sibling spans that all share traceId but have distinct spanIds. This
        // exercises the per-batch dedupe in ProcessTraces — each span should NOT bump
        // the registration counter.
        using var scopeStream = new MemoryStream();
        for (var i = 0; i < spanCount; i++)
        {
            var spanId = i.ToString("X16");
            using var spanStream = new MemoryStream();
            WriteField(spanStream, 1, Convert.FromHexString(traceId));
            WriteField(spanStream, 2, Convert.FromHexString(spanId));
            WriteStringField(spanStream, 5, $"sut-batch-op-{i}");
            WriteVarintField(spanStream, 6, SpanKindConsumer);
            WriteFixed64Field(spanStream, 7, TestStartTimeUnixNano);
            WriteFixed64Field(spanStream, 8, TestEndTimeUnixNano);
            WriteField(scopeStream, 2, spanStream.ToArray());
        }

        using var resourceStream = new MemoryStream();
        WriteField(resourceStream, 2, scopeStream.ToArray());
        using var exportStream = new MemoryStream();
        WriteField(exportStream, 1, resourceStream.ToArray());
        return exportStream.ToArray();
    }

    private static byte[] BuildLinkedTraceExportRequest(
        string derivedTraceId,
        string derivedSpanId,
        string spanName,
        string sourceTraceId,
        string sourceSpanId)
    {
        using var exportStream = new MemoryStream();
        var resourceSpans = BuildResourceSpans(
            BuildScopeSpans(
                BuildSpan(derivedTraceId, derivedSpanId, spanName, sourceTraceId, sourceSpanId)));
        WriteField(exportStream, 1, resourceSpans);
        return exportStream.ToArray();
    }

    private static byte[] BuildResourceSpans(byte[] scopeSpans)
    {
        using var stream = new MemoryStream();
        WriteField(stream, 2, scopeSpans);
        return stream.ToArray();
    }

    private static byte[] BuildScopeSpans(byte[] span)
    {
        using var stream = new MemoryStream();
        WriteField(stream, 2, span);
        return stream.ToArray();
    }

    private static byte[] BuildSpan(
        string derivedTraceId,
        string derivedSpanId,
        string spanName,
        string sourceTraceId,
        string sourceSpanId)
    {
        using var stream = new MemoryStream();
        WriteField(stream, 1, Convert.FromHexString(derivedTraceId));
        WriteField(stream, 2, Convert.FromHexString(derivedSpanId));
        WriteStringField(stream, 5, spanName);
        WriteVarintField(stream, 6, SpanKindConsumer);
        WriteFixed64Field(stream, 7, TestStartTimeUnixNano); // start_time_unix_nano
        WriteFixed64Field(stream, 8, TestEndTimeUnixNano); // end_time_unix_nano
        WriteField(stream, 13, BuildSpanLink(sourceTraceId, sourceSpanId));
        return stream.ToArray();
    }

    private static byte[] BuildSpanLink(string traceId, string spanId)
    {
        using var stream = new MemoryStream();
        WriteField(stream, 1, Convert.FromHexString(traceId));
        WriteField(stream, 2, Convert.FromHexString(spanId));
        return stream.ToArray();
    }

    private static void WriteStringField(MemoryStream stream, int fieldNumber, string value)
    {
        WriteField(stream, fieldNumber, System.Text.Encoding.UTF8.GetBytes(value));
    }

    private static void WriteVarintField(MemoryStream stream, int fieldNumber, ulong value)
    {
        WriteTag(stream, fieldNumber, 0);
        WriteVarint(stream, value);
    }

    private static void WriteFixed64Field(MemoryStream stream, int fieldNumber, ulong value)
    {
        WriteTag(stream, fieldNumber, 1);
        stream.Write(BitConverter.GetBytes(value));
    }

    private static void WriteField(MemoryStream stream, int fieldNumber, byte[] value)
    {
        WriteTag(stream, fieldNumber, 2);
        WriteVarint(stream, (ulong)value.Length);
        stream.Write(value);
    }

    private static void WriteTag(MemoryStream stream, int fieldNumber, int wireType)
    {
        WriteVarint(stream, (ulong)((fieldNumber << 3) | wireType));
    }

    private static void WriteVarint(MemoryStream stream, ulong value)
    {
        // Manual protobuf encoding keeps this regression test self-contained and avoids
        // introducing a protobuf dependency just to build one OTLP payload.
        // Standard protobuf wire-format varint encoding: integers are emitted in
        // 7-bit chunks and the high bit marks that another byte follows.
        do
        {
            var current = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                current |= 0x80;
            }

            stream.WriteByte(current);
        } while (value != 0);
    }
}
