using System.Diagnostics;
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
    public async Task Receiver_ParsedLinkedTrace_RegistersAgainstOwningTest()
    {
        var collector = ActivityCollector.Current;
        await Assert.That(collector).IsNotNull();

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
