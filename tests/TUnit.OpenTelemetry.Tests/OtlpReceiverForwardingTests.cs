using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.OpenTelemetry.Receiver;
using TUnit.OpenTelemetry.Tests.Helpers;

namespace TUnit.OpenTelemetry.Tests;

public class OtlpReceiverForwardingTests
{
    [Test]
    public async Task Receiver_WithUpstream_ForwardsTraceBody()
    {
        await using var upstream = new OtlpTraceCaptureServer();
        upstream.Start();

        await using var receiver = new OtlpReceiver(upstreamEndpoint: $"http://127.0.0.1:{upstream.Port}");
        receiver.Start();

        var sourceName = $"TUnit.ForwardingTest.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri($"http://127.0.0.1:{receiver.Port}/v1/traces");
                o.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();

        using (source.StartActivity("forwarded-op"))
        {
        }

        provider!.ForceFlush(5000);

        var captured = await upstream.WaitForRequestAsync("/v1/traces");
        await Assert.That(captured.Body.Length).IsGreaterThan(0);

        var parsed = OtlpTraceParser.Parse(captured.Body);
        await Assert.That(parsed.Any(s => s.Name == "forwarded-op")).IsTrue();
    }

    [Test]
    public async Task Receiver_WithoutUpstream_DoesNotForward()
    {
        await using var upstream = new OtlpTraceCaptureServer();
        upstream.Start();

        await using var receiver = new OtlpReceiver();
        receiver.Start();

        using var client = new HttpClient();
        using var content = new ByteArrayContent([0x00]);
        await client.PostAsync($"http://127.0.0.1:{receiver.Port}/v1/traces", content);
        await receiver.WhenIdle();

        await Assert.That(upstream.HasRequest("/v1/traces")).IsFalse();
    }
}
