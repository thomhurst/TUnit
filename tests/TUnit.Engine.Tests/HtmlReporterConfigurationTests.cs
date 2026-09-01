using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
using TUnit.Core.Settings;
using TUnit.Engine.Reporters.Aggregation;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Tests;

[NotInParallel]
public class HtmlReporterConfigurationTests
{
    private bool _htmlReportEnabled;
    private bool _jsonReportEnabled;
    private bool _artifactUploadEnabled;
    private string? _disableHtmlReporter;
    private string? _disableJsonReport;
    private string? _disableArtifactUpload;
    private string? _aggregateReports;
    private string? _aggregateDirectory;

    [Before(HookType.Test)]
    public void SnapshotConfiguration()
    {
        _htmlReportEnabled = TUnitSettings.Default.Reporting.HtmlReportEnabled;
        _jsonReportEnabled = TUnitSettings.Default.Reporting.JsonReportEnabled;
        _artifactUploadEnabled = TUnitSettings.Default.Reporting.ArtifactUploadEnabled;
        _disableHtmlReporter = Environment.GetEnvironmentVariable("TUNIT_DISABLE_HTML_REPORTER");
        _disableJsonReport = Environment.GetEnvironmentVariable("TUNIT_DISABLE_JSON_REPORT");
        _disableArtifactUpload = Environment.GetEnvironmentVariable("TUNIT_DISABLE_ARTIFACT_UPLOAD");
        _aggregateReports = Environment.GetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS");
        _aggregateDirectory = Environment.GetEnvironmentVariable("TUNIT_AGGREGATE_DIR");

        Environment.SetEnvironmentVariable("TUNIT_DISABLE_HTML_REPORTER", null);
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_JSON_REPORT", null);
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_ARTIFACT_UPLOAD", null);
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", null);
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", null);
    }

    [After(HookType.Test)]
    public void RestoreConfiguration()
    {
        TUnitSettings.Default.Reporting.HtmlReportEnabled = _htmlReportEnabled;
        TUnitSettings.Default.Reporting.JsonReportEnabled = _jsonReportEnabled;
        TUnitSettings.Default.Reporting.ArtifactUploadEnabled = _artifactUploadEnabled;
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_HTML_REPORTER", _disableHtmlReporter);
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_JSON_REPORT", _disableJsonReport);
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_ARTIFACT_UPLOAD", _disableArtifactUpload);
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", _aggregateReports);
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", _aggregateDirectory);
    }

    [Test]
    public async Task Programmatic_Settings_Can_Disable_Html_Reporting_Features()
    {
        TUnitSettings.Default.Reporting.HtmlReportEnabled = false;
        TUnitSettings.Default.Reporting.JsonReportEnabled = false;
        TUnitSettings.Default.Reporting.ArtifactUploadEnabled = false;

        await Assert.That(HtmlReporter.IsHtmlReportEnabled()).IsFalse();
        await Assert.That(HtmlReporter.IsJsonReportEnabled()).IsFalse();
        await Assert.That(HtmlReporter.IsArtifactUploadEnabled()).IsFalse();
    }

    [Test]
    public async Task Disable_Environment_Variables_Take_Precedence()
    {
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_HTML_REPORTER", "true");
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_JSON_REPORT", "1");
        Environment.SetEnvironmentVariable("TUNIT_DISABLE_ARTIFACT_UPLOAD", "yes");

        await Assert.That(HtmlReporter.IsHtmlReportEnabled()).IsFalse();
        await Assert.That(HtmlReporter.IsJsonReportEnabled()).IsFalse();
        await Assert.That(HtmlReporter.IsArtifactUploadEnabled()).IsFalse();
    }

    [Test]
    public async Task Disabled_Html_Report_Stops_Activity_Collection_After_Discovery(CancellationToken cancellationToken)
    {
        using var reporter = new HtmlReporter(new MockExtension());
        await reporter.BeforeRunAsync(cancellationToken);
        reporter.HasActivityCollector.ShouldBeTrue();

        TUnitSettings.Default.Reporting.HtmlReportEnabled = false;
        await reporter.ConsumeAsync(reporter, null!, cancellationToken);

        reporter.HasActivityCollector.ShouldBeFalse();
    }

    [Test]
    public async Task Html_Report_Setting_Is_Resolved_Per_Session(CancellationToken cancellationToken)
    {
        using var reporter = new HtmlReporter(new MockExtension());

        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionStartingAsync(null!);
        TUnitSettings.Default.Reporting.HtmlReportEnabled = false;
        reporter.IsHtmlReportEnabledForRun().ShouldBeFalse();

        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionStartingAsync(null!);
        TUnitSettings.Default.Reporting.HtmlReportEnabled = true;
        reporter.IsHtmlReportEnabledForRun().ShouldBeTrue();
    }

    [Test]
    public async Task Test_Updates_Are_Cleared_Between_Sessions(CancellationToken cancellationToken)
    {
        using var reporter = new HtmlReporter(new MockExtension());

        await reporter.OnTestSessionStartingAsync(null!);
        await reporter.ConsumeAsync(reporter, CreatePassedUpdate("first"), cancellationToken);
        reporter.BuildReportData().Groups.SelectMany(x => x.Tests).Single().Id.ShouldBe("first");

        await reporter.OnTestSessionStartingAsync(null!);
        await reporter.ConsumeAsync(reporter, CreatePassedUpdate("second"), cancellationToken);

        reporter.BuildReportData().Groups.SelectMany(x => x.Tests).Single().Id.ShouldBe("second");
    }

    [Test]
    public async Task Disabled_Artifact_Upload_Does_Not_Publish_Session_File_Artifact(CancellationToken cancellationToken)
    {
        TUnitSettings.Default.Reporting.ArtifactUploadEnabled = false;
        var reporter = new HtmlReporter(new MockExtension());
        var messageBus = new CapturingMessageBus();
        reporter.SetMessageBus(messageBus);

        await reporter.PublishArtifactAsync("report.html", new SessionUid("session"), cancellationToken);

        messageBus.Published.ShouldBeEmpty();
    }

    [Test]
    public async Task Disabled_Json_Report_Does_Not_Write_Local_Or_Aggregation_Sidecars(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            Directory.CreateDirectory(tempDirectory);
            var reporter = new HtmlReporter(new MockExtension());

            TUnitSettings.Default.Reporting.JsonReportEnabled = true;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancellationToken);
            File.Exists(HtmlReporter.GetSidecarPath(htmlPath)).ShouldBeTrue();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").ShouldNotBeEmpty();

            TUnitSettings.Default.Reporting.JsonReportEnabled = false;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancellationToken);

            File.Exists(HtmlReporter.GetSidecarPath(htmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static ReportData CreateReportData() => new()
    {
        AssemblyName = "Tests",
        MachineName = "machine",
        Timestamp = DateTimeOffset.UtcNow.ToString("O"),
        TUnitVersion = "1.0.0",
        OperatingSystem = "test",
        RuntimeVersion = "test",
        Summary = new ReportSummary(),
        Groups = [],
    };

    private static TestNodeUpdateMessage CreatePassedUpdate(string id) => new(
        new SessionUid("session"),
        new TestNode
        {
            Uid = new TestNodeUid(id),
            DisplayName = id,
            Properties = new PropertyBag(PassedTestNodeStateProperty.CachedInstance),
        });
}
