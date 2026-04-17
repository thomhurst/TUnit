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
            traceId = activity!.TraceId.ToString().ToUpperInvariant();
            collector!.RegisterExternalTrace(traceId);
        }

        provider!.ForceFlush(5000);
        await receiver.WhenIdle();

        var span = collector!.GetAllSpans().FirstOrDefault(s =>
            s.TraceId == traceId && s.Name == "sut-external-op");

        await Assert.That(span).IsNotNull();
    }
}
