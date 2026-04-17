using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Engine.Reporters.Html;

namespace TUnit.OpenTelemetry.Tests;

public class ActivityCollectorIngestionTests
{
    [Test]
    public async Task IngestExternalSpan_KnownTrace_AppearsInGetAllSpans()
    {
        using var collector = new ActivityCollector();
        collector.Start();

        var traceId = UniqueTraceId();
        collector.RegisterExternalTrace(traceId);

        collector.IngestExternalSpan(MakeSpan(traceId, "ABCD567812345678", "external-op"));

        var ours = collector.GetAllSpans().Where(s => s.TraceId == traceId).ToList();
        await Assert.That(ours.Count).IsEqualTo(1);
        await Assert.That(ours[0].Name).IsEqualTo("external-op");
    }

    [Test]
    public async Task IngestExternalSpan_UnknownTrace_IsDropped()
    {
        using var collector = new ActivityCollector();
        collector.Start();

        var unknownTraceId = UniqueTraceId();
        collector.IngestExternalSpan(MakeSpan(unknownTraceId, "ABCD567812345678", "op"));

        var ours = collector.GetAllSpans().Where(s => s.TraceId == unknownTraceId).ToList();
        await Assert.That(ours.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IngestExternalSpan_ExceedsPerTraceCap_Drops()
    {
        using var collector = new ActivityCollector();
        collector.Start();

        var traceId = UniqueTraceId();
        collector.RegisterExternalTrace(traceId);

        var cap = ActivityCollector.MaxExternalSpans;
        var attempts = cap + 50;
        for (var i = 0; i < attempts; i++)
        {
            collector.IngestExternalSpan(MakeSpan(traceId, $"{i:X16}", $"op-{i}"));
        }

        var ours = collector.GetAllSpans().Where(s => s.TraceId == traceId).ToList();
        await Assert.That(ours.Count).IsEqualTo(cap);
    }

    [Test]
    public async Task Current_IsNonNull_DuringTestSession()
    {
        // HtmlReporter starts its own collector in BeforeRunAsync, so Current
        // is populated before this test runs. We don't assert *which* collector
        // it is — parallel tests may compete — only that the wiring is alive.
        await Assert.That(ActivityCollector.Current).IsNotNull();
    }

    [Test]
    public async Task IngestExternalSpan_TraceIdCaseMismatch_StillCorrelates()
    {
        using var collector = new ActivityCollector();
        collector.Start();

        // Activity.TraceId.ToString() produces lowercase; the OTLP parser produces uppercase.
        // Registration and ingestion must correlate across that case boundary.
        var registeredLower = UniqueTraceId().ToLowerInvariant();
        var ingestedUpper = registeredLower.ToUpperInvariant();
        collector.RegisterExternalTrace(registeredLower);

        collector.IngestExternalSpan(MakeSpan(ingestedUpper, "ABCD567812345678", "op"));

        var spans = collector.GetAllSpans()
            .Where(s => string.Equals(s.TraceId, registeredLower, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await Assert.That(spans.Count).IsEqualTo(1);
    }

    [Test]
    public async Task IngestExternalSpan_UnknownParent_FallsBackToPerTraceCap()
    {
        using var collector = new ActivityCollector();
        collector.Start();

        var traceId = UniqueTraceId();
        collector.RegisterExternalTrace(traceId);

        // No test case span registered, so ParentSpanId falls through to per-trace cap.
        collector.IngestExternalSpan(MakeSpan(traceId, "AAAA000000000001", "op", parentSpanId: "UNKNOWNPARENTID0"));

        var ours = collector.GetAllSpans().Where(s => s.TraceId == traceId).ToList();
        await Assert.That(ours.Count).IsEqualTo(1);
    }

    private static string UniqueTraceId() => Guid.NewGuid().ToString("N").ToUpperInvariant();

    private static SpanData MakeSpan(string traceId, string spanId, string name, string? parentSpanId = null) => new()
    {
        TraceId = traceId,
        SpanId = spanId,
        ParentSpanId = parentSpanId,
        Name = name,
        Source = "external",
        Kind = "Internal",
        Status = "Ok",
        StartTimeMs = 1000,
        DurationMs = 10,
    };
}
