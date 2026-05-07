#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
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
        var later = CreateReportTestResult("Later", "2026-05-07T09:26:25.0000000Z");
        var earlier = CreateReportTestResult("Earlier", "2026-05-07T09:26:24.0000000Z");
        var sameTimeButLaterName = CreateReportTestResult("Zeta", "2026-05-07T09:26:24.0000000Z");

        var ordered = HtmlReporter.OrderTestsForDisplay([later, sameTimeButLaterName, earlier]);

        ordered.Select(static test => test.DisplayName).ShouldBe(["Earlier", "Zeta", "Later"]);
    }

    [Test]
    public void GenerateHtml_Includes_ClassTimeline_TestCaseRendering()
    {
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
        });

        html.ShouldContain("function collapseTestBodySpans(spans)");
        html.ShouldContain("const filtered = collapseTestBodySpans(getDescendants(allSpans, suite.spanId));");
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

    private static ReportTestResult CreateReportTestResult(string displayName, string? startTime) => new()
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
