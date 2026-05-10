#pragma warning disable TPEXP

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Tests;

public class HtmlReporterTests
{
    [Test]
    public void HtmlReporter_Implements_IDataProducer()
    {
        var reporter = new HtmlReporter(new MockExtension());
        reporter.ShouldBeAssignableTo<Microsoft.Testing.Platform.Extensions.Messages.IDataProducer>();
    }

    [Test]
    public void HtmlReporter_DataTypesProduced_Contains_SessionFileArtifact()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var producer = (Microsoft.Testing.Platform.Extensions.Messages.IDataProducer)reporter;
        producer.DataTypesProduced.ShouldContain(typeof(SessionFileArtifact));
    }

    [Test]
    public async Task PublishArtifactAsync_Publishes_SessionFileArtifact_When_SessionContext_Set_And_File_Exists()
    {
        // Arrange
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        reporter.SetMessageBus(bus);

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            await reporter.PublishArtifactAsync(tempFile, new SessionUid("test-session-1"), CancellationToken.None);

            // Assert
            bus.Published.Count.ShouldBe(1);
            var artifact = bus.Published[0].Data.ShouldBeOfType<Microsoft.Testing.Platform.Extensions.Messages.SessionFileArtifact>();
            artifact.FileInfo.FullName.ShouldBe(new FileInfo(tempFile).FullName);
            artifact.DisplayName.ShouldBe("HTML Test Report");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task PublishArtifactAsync_Is_NoOp_When_MessageBus_Not_Injected()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        // Intentionally not calling reporter.SetMessageBus(bus)

        var tempFile = Path.GetTempFileName();
        try
        {
            await reporter.PublishArtifactAsync(tempFile, new SessionUid("test-session-1"), CancellationToken.None);
            bus.Published.ShouldBeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }


    [Test]
    public void FilterAdditionalTraceIds_Removes_Primary_Trace_CaseInsensitive()
    {
        var primary = "abcdef0123456789abcdef0123456789";
        var linked = "1111111111111111aaaaaaaaaaaaaaaa";
        var all = new[] { primary.ToUpperInvariant(), linked };

        var result = HtmlReporter.FilterAdditionalTraceIds(all, primary);

        result.ShouldBe(new[] { linked });
    }

    [Test]
    public void FilterAdditionalTraceIds_Returns_Input_When_Primary_Null()
    {
        var all = new[] { "aaaa", "bbbb" };

        var result = HtmlReporter.FilterAdditionalTraceIds(all, primaryTraceId: null);

        result.ShouldBeSameAs(all);
    }

    [Test]
    public void FilterAdditionalTraceIds_Returns_Input_When_No_Match()
    {
        var all = new[] { "aaaa", "bbbb" };

        var result = HtmlReporter.FilterAdditionalTraceIds(all, "cccc");

        result.ShouldBeSameAs(all);
    }

    [Test]
    public void FilterAdditionalTraceIds_Returns_Empty_When_Only_Primary()
    {
        var primary = "abcdef0123456789abcdef0123456789";

        var result = HtmlReporter.FilterAdditionalTraceIds(new[] { primary }, primary);

        result.ShouldBeEmpty();
    }

    [Test]
    public void OrderTestsForDisplay_SortsByStartTime_ThenName()
    {
        var later = CreateTestResultWithStartTime("Later", "2026-05-07T09:26:25.0000000Z");
        var earlier = CreateTestResultWithStartTime("Earlier", "2026-05-07T09:26:24.0000000Z");
        var sameTimeButLaterName = CreateTestResultWithStartTime("Zeta", "2026-05-07T09:26:24.0000000Z");

        var ordered = HtmlReporter.OrderTestsForDisplay([later, sameTimeButLaterName, earlier]);

        ordered.Select(static test => test.DisplayName).ShouldBe(["Earlier", "Zeta", "Later"]);
    }

    [Test]
    public void GenerateHtml_RoundTrips_TestBodySpans_AndChildren_Through_EmbeddedData()
    {
        // Server-side data path only — the client-side collapseTestBodySpans JS runs in the
        // browser and is not exercised here. This test pins down the contract the JS relies
        // on: a 'test body' span with children survives serialisation into the embedded
        // JSON so the JS can re-parent children to the test-case span at render time.
        const string traceId = "0123456789abcdef0123456789abcdef";
        var spans = new[]
        {
            new SpanData
            {
                TraceId = traceId, SpanId = "aaaaaaaaaaaaaaaa", Name = "test body",
                Source = "TUnit", Kind = "Internal", Status = "Ok",
            },
            new SpanData
            {
                TraceId = traceId, SpanId = "bbbbbbbbbbbbbbbb", ParentSpanId = "aaaaaaaaaaaaaaaa",
                Name = "wiremock-call", Source = "TUnit", Kind = "Client", Status = "Ok",
            },
        };

        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups = [],
            Spans = spans,
        });

        var embedded = ExtractEmbeddedReportJson(html);
        embedded.ShouldContain("\"name\":\"test body\"");
        embedded.ShouldContain("\"name\":\"wiremock-call\"");
        embedded.ShouldContain("\"parentSpanId\":\"aaaaaaaaaaaaaaaa\"");
    }

    private static string ExtractEmbeddedReportJson(string html)
    {
        // The renderer embeds ReportData as gzip+base64 inside <script id="test-data" ...>.
        var match = Regex.Match(
            html,
            "<script id=\"test-data\"[^>]*>(?<payload>[A-Za-z0-9+/=]+)</script>",
            RegexOptions.Singleline);
        match.Success.ShouldBeTrue("Expected embedded test-data script in rendered HTML.");
        var compressed = Convert.FromBase64String(match.Groups["payload"].Value);
        using var ms = new MemoryStream(compressed);
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gz, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Test]
    public async Task PublishArtifactAsync_Publishes_With_Correct_SessionUid()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        reporter.SetMessageBus(bus);

        var tempFile = Path.GetTempFileName();
        try
        {
            var uid = new SessionUid("my-session-42");
            await reporter.PublishArtifactAsync(tempFile, uid, CancellationToken.None);

            var artifact = bus.Published[0].Data.ShouldBeOfType<SessionFileArtifact>();
            artifact.SessionUid.Value.ShouldBe("my-session-42");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void ClassTimelineAttribute_Exposes_Mode_And_ScopeType()
    {
        var full = new ClassTimelineAttribute(TimelineMode.FullExecution);
        full.Mode.ShouldBe(TimelineMode.FullExecution);
        full.ScopeType.ShouldBe(typeof(ClassTimelineAttribute));

        var collapsed = new ClassTimelineAttribute(TimelineMode.Collapsed);
        collapsed.Mode.ShouldBe(TimelineMode.Collapsed);
    }

    [Test]
    public void GenerateHtml_RoundTrips_ClassTimeline_CustomProperty_OnTest()
    {
        // The JS reads group.tests[0].customProperties for tunit.report.timeline; this
        // pins the contract that the property survives serialisation into the embedded JSON.
        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = "BddFlow",
                    Namespace = "Sample",
                    Summary = new ReportSummary(),
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "t1", DisplayName = "t1", MethodName = "t1",
                            ClassName = "BddFlow", Status = "passed",
                            CustomProperties =
                            [
                                new ReportKeyValue { Key = ClassTimelineAttribute.ClassTimelinePropertyKey, Value = nameof(TimelineMode.FullExecution) }
                            ],
                        },
                    ],
                },
            ],
        });

        var embedded = ExtractEmbeddedReportJson(html);
        embedded.ShouldContain("\"key\":\"tunit.report.timeline\"");
        embedded.ShouldContain("\"value\":\"FullExecution\"");
    }

    private static ReportTestResult CreateTestResultWithStartTime(string displayName, string? startTime) => new()
    {
        Id = displayName,
        DisplayName = displayName,
        MethodName = displayName,
        ClassName = "SampleTests",
        Status = "passed",
        DurationMs = 1,
        StartTime = startTime,
        EndTime = startTime,
        RetryAttempt = 0,
    };
}
