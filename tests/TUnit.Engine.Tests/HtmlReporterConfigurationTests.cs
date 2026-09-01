using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
using TUnit.Core.Settings;
using TUnit.Engine.Reporters;
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
        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionStartingAsync(null!);
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
    public async Task Activity_Collection_Starts_Before_Discovery_Reenables_Reporting(CancellationToken cancellationToken)
    {
        TUnitSettings.Default.Reporting.HtmlReportEnabled = false;
        using var reporter = new HtmlReporter(new MockExtension());

        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionStartingAsync(null!);
        reporter.HasActivityCollector.ShouldBeTrue();

        var activity = TUnitActivitySource.StartLifecycleActivity(TUnitActivitySource.SpanTestSession);
        TUnitSettings.Default.Reporting.HtmlReportEnabled = true;
        reporter.IsHtmlReportEnabledForRun().ShouldBeTrue();
        TUnitActivitySource.StopActivity(activity);

        reporter.StopActivityCollection();
        var spans = reporter.BuildReportData().Spans;
        spans.ShouldNotBeNull();
        spans.ShouldContain(span => span.SpanType == TUnitActivitySource.SpanTestSession);
    }

    [Test]
    public async Task Activity_Collection_Is_Recreated_Between_Sessions(CancellationToken cancellationToken)
    {
        TUnitSettings.Default.Reporting.HtmlReportEnabled = true;
        using var reporter = new HtmlReporter(new MockExtension());

        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionStartingAsync(null!);
        reporter.HasActivityCollector.ShouldBeTrue();

        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionFinishingAsync(null!);
        reporter.HasActivityCollector.ShouldBeFalse();

        cancellationToken.ThrowIfCancellationRequested();
        await reporter.OnTestSessionStartingAsync(null!);
        reporter.HasActivityCollector.ShouldBeTrue();

        var activity = TUnitActivitySource.StartLifecycleActivity(TUnitActivitySource.SpanTestSession);
        TUnitActivitySource.StopActivity(activity);
        reporter.StopActivityCollection();
        var spans = reporter.BuildReportData().Spans;
        spans.ShouldNotBeNull();
        spans.ShouldContain(span => span.SpanType == TUnitActivitySource.SpanTestSession);
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
    public async Task GitHub_Summary_Suppression_Is_Reset_Between_Sessions(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", Path.Combine(tempDirectory, "aggregate"));

        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var reporter = new HtmlReporter(new MockExtension());
            var githubReporter = new GitHubReporter(new MockExtension());
            reporter.SetGitHubReporter(githubReporter);

            await reporter.TryWriteSidecarAndAggregateAsync(
                CreateReportData(),
                Path.Combine(tempDirectory, "suite-report.html"),
                cancellationToken);
            githubReporter.SuppressPerSuiteSummary.ShouldBeTrue();

            await reporter.OnTestSessionStartingAsync(null!);

            githubReporter.SuppressPerSuiteSummary.ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
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
    public async Task Disabled_Json_Report_Removes_Stale_Aggregation_Outputs(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var removedHtmlPath = Path.Combine(tempDirectory, "removed-report.html");
        var remainingHtmlPath = Path.Combine(tempDirectory, "remaining-report.html");
        var mergedReportPath = Path.Combine(aggregationDirectory, ReportDataJson.MergedReportFileName);
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            Directory.CreateDirectory(tempDirectory);
            var reporter = new HtmlReporter(new MockExtension());

            TUnitSettings.Default.Reporting.JsonReportEnabled = true;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemovedSuiteMarker"), removedHtmlPath, cancellationToken);
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemainingSuiteMarker"), remainingHtmlPath, cancellationToken);
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(2);

            TUnitSettings.Default.Reporting.JsonReportEnabled = false;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemovedSuiteMarker"), removedHtmlPath, cancellationToken);

            File.Exists(HtmlReporter.GetSidecarPath(removedHtmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
            var mergedReport = File.ReadAllText(mergedReportPath);
            mergedReport.ShouldNotContain("RemovedSuiteMarker");
            mergedReport.ShouldContain("RemainingSuiteMarker");

            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemainingSuiteMarker"), remainingHtmlPath, cancellationToken);

            File.Exists(HtmlReporter.GetSidecarPath(remainingHtmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").ShouldBeEmpty();
            File.Exists(mergedReportPath).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Test]
    public async Task Disabled_Html_Report_Removes_Stale_Aggregation_Outputs(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var disabledHtmlPath = Path.Combine(tempDirectory, "disabled-report.html");
        var remainingHtmlPath = Path.Combine(tempDirectory, "remaining-report.html");
        var mergedReportPath = Path.Combine(aggregationDirectory, ReportDataJson.MergedReportFileName);
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var reporter = new HtmlReporter(new MockExtension());
            reporter.SetOutputPath(disabledHtmlPath);
            var disabledAssemblyName = reporter.BuildReportData().AssemblyName;

            TUnitSettings.Default.Reporting.JsonReportEnabled = true;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(disabledAssemblyName), disabledHtmlPath, cancellationToken);
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemainingSuiteMarker"), remainingHtmlPath, cancellationToken);

            TUnitSettings.Default.Reporting.HtmlReportEnabled = false;
            await reporter.OnTestSessionFinishingAsync(null!);

            File.Exists(HtmlReporter.GetSidecarPath(disabledHtmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
            var mergedReport = File.ReadAllText(mergedReportPath);
            mergedReport.ShouldNotContain(disabledAssemblyName);
            mergedReport.ShouldContain("RemainingSuiteMarker");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Test]
    public async Task Shared_Sidecar_Is_Published_While_Waiting_And_Restored_After_Cleanup(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "suite-report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var reporter = new HtmlReporter(new MockExtension());
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable);
            var aggregationLock = await aggregator!.AcquireLockAsync(cancellationToken);

            Task writeTask;
            using (aggregationLock)
            {
                writeTask = reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancellationToken);

                File.Exists(HtmlReporter.GetSidecarPath(htmlPath)).ShouldBeTrue();
                var sharedSidecarPath = Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Single();
                File.Delete(sharedSidecarPath);
            }

            await writeTask;

            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
            File.ReadAllText(Path.Combine(aggregationDirectory, ReportDataJson.MergedReportFileName)).ShouldContain("Tests");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static ReportData CreateReportData(string assemblyName = "Tests") => new()
    {
        AssemblyName = assemblyName,
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
