using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.OpenTelemetry.Receiver;
using TUnit.OpenTelemetry.Tests.Helpers;

namespace TUnit.OpenTelemetry.Tests;

public class OtlpTraceParserTests
{
    [Test]
    public async Task Parse_SingleSpan_ExtractsIdsAndName()
    {
        await using var server = new OtlpTraceCaptureServer();
        server.Start();

        var sourceName = $"TUnit.Tests.Parser.{Guid.NewGuid():N}";
        var endpoint = $"http://127.0.0.1:{server.Port}/v1/traces";

        string expectedTraceId;
        string expectedSpanId;

        using (var source = new ActivitySource(sourceName))
        using (var provider = Sdk.CreateTracerProviderBuilder()
                   .AddSource(sourceName)
                   .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("parser-test-svc"))
                   .AddOtlpExporter(o =>
                   {
                       o.Endpoint = new Uri(endpoint);
                       o.Protocol = OtlpExportProtocol.HttpProtobuf;
                   })
                   .Build())
        {
            using var activity = source.StartActivity("parse-me", ActivityKind.Server);
            activity!.SetTag("probe.key", "probe-value");
            expectedTraceId = activity.TraceId.ToString();
            expectedSpanId = activity.SpanId.ToString();
        }

        var request = await server.WaitForRequestAsync("/v1/traces");

        var spans = OtlpTraceParser.Parse(request.Body);

        await Assert.That(spans).Count().IsEqualTo(1);

        var span = spans[0];
        // Parser emits lowercase to match Activity.TraceId/SpanId serialization.
        await Assert.That(span.TraceId).IsEqualTo(expectedTraceId);
        await Assert.That(span.SpanId).IsEqualTo(expectedSpanId);
        await Assert.That(span.Name).IsEqualTo("parse-me");
        await Assert.That(span.Kind).IsEqualTo(2); // SERVER
        await Assert.That(span.ResourceName).IsEqualTo("parser-test-svc");
        var probeAttr = span.Attributes.FirstOrDefault(kv => kv.Key == "probe.key");
        await Assert.That(probeAttr.Value).IsEqualTo("probe-value");
    }

    [Test]
    public async Task Parse_ParentChildSpans_LinksViaParentSpanId()
    {
        await using var server = new OtlpTraceCaptureServer();
        server.Start();

        var sourceName = $"TUnit.Tests.Parser.{Guid.NewGuid():N}";

        string parentSpanId;

        using (var source = new ActivitySource(sourceName))
        using (var provider = Sdk.CreateTracerProviderBuilder()
                   .AddSource(sourceName)
                   .AddOtlpExporter(o =>
                   {
                       o.Endpoint = new Uri($"http://127.0.0.1:{server.Port}/v1/traces");
                       o.Protocol = OtlpExportProtocol.HttpProtobuf;
                   })
                   .Build())
        {
            using var parent = source.StartActivity("parent")!;
            parentSpanId = parent.SpanId.ToString();
            using var child = source.StartActivity("child");
        }

        var request = await server.WaitForRequestAsync("/v1/traces");
        var spans = OtlpTraceParser.Parse(request.Body);

        var childSpan = spans.Single(s => s.Name == "child");
        await Assert.That(childSpan.ParentSpanId).IsEqualTo(parentSpanId);
    }

    [Test]
    public async Task Parse_ErrorStatus_ExtractsCodeAndMessage()
    {
        await using var server = new OtlpTraceCaptureServer();
        server.Start();

        var sourceName = $"TUnit.Tests.Parser.{Guid.NewGuid():N}";

        using (var source = new ActivitySource(sourceName))
        using (var provider = Sdk.CreateTracerProviderBuilder()
                   .AddSource(sourceName)
                   .AddOtlpExporter(o =>
                   {
                       o.Endpoint = new Uri($"http://127.0.0.1:{server.Port}/v1/traces");
                       o.Protocol = OtlpExportProtocol.HttpProtobuf;
                   })
                   .Build())
        {
            using var activity = source.StartActivity("failing")!;
            activity.SetStatus(ActivityStatusCode.Error, "oh no");
        }

        var request = await server.WaitForRequestAsync("/v1/traces");
        var spans = OtlpTraceParser.Parse(request.Body);

        var span = spans.Single();
        await Assert.That(span.StatusCode).IsEqualTo(2); // ERROR
        await Assert.That(span.StatusMessage).IsEqualTo("oh no");
    }

    [Test]
    public async Task Parse_Empty_ReturnsEmptyList()
    {
        var spans = OtlpTraceParser.Parse(Array.Empty<byte>());
        await Assert.That(spans).Count().IsEqualTo(0);
    }
}
