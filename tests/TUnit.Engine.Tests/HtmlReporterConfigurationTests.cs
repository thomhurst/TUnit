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
    public async Task GitHub_Report_State_Is_Reset_Between_Sessions(CancellationToken cancellationToken)
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
            githubReporter.ArtifactUrl = "https://example.com/old-artifact";
            githubReporter.ShowArtifactUploadTip = true;

            await reporter.OnTestSessionStartingAsync(null!);

            githubReporter.SuppressPerSuiteSummary.ShouldBeFalse();
            githubReporter.ArtifactUrl.ShouldBeNull();
            githubReporter.ShowArtifactUploadTip.ShouldBeFalse();
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
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;

            TUnitSettings.Default.Reporting.JsonReportEnabled = true;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemovedSuiteMarker"), removedHtmlPath, cancellationToken);
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemainingSuiteMarker"), remainingHtmlPath, cancellationToken);
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(2);

            TUnitSettings.Default.Reporting.JsonReportEnabled = false;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemovedSuiteMarker"), removedHtmlPath, cancellationToken);

            File.Exists(HtmlReporter.GetSidecarPath(removedHtmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(2);
            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("RemainingSuiteMarker");
            var mergedReport = File.ReadAllText(mergedReportPath);
            mergedReport.ShouldNotContain("RemovedSuiteMarker");
            mergedReport.ShouldContain("RemainingSuiteMarker");

            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemainingSuiteMarker"), remainingHtmlPath, cancellationToken);

            File.Exists(HtmlReporter.GetSidecarPath(remainingHtmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(2);
            aggregator.ReadAllSidecars().ShouldBeEmpty();
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
    public async Task Cancelled_Session_Still_Removes_Disabled_Report_Sidecars(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "cancelled-report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var reporter = new HtmlReporter(new MockExtension());
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;

            TUnitSettings.Default.Reporting.JsonReportEnabled = true;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancellationToken);

            using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancelled.Cancel();
            TUnitSettings.Default.Reporting.JsonReportEnabled = false;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancelled.Token);

            File.Exists(HtmlReporter.GetSidecarPath(htmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
            aggregator.ReadAllSidecars().ShouldBeEmpty();
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
    public async Task Disabled_Marker_Excludes_Stale_Shared_Sidecar(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", tempDirectory);

        try
        {
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            var reportData = CreateReportData("DisabledSuiteMarker");
            aggregator.WriteSidecar(ReportDataJson.SerializeToBytes(reportData), reportData.AssemblyName, "suite");
            var staleGeneration = aggregator.ReadSidecarGeneration(reportData.AssemblyName, "suite");

            var replacement = CreateReportData(reportData.AssemblyName, "replacement-machine");
            aggregator.WriteSidecar(ReportDataJson.SerializeToBytes(replacement), replacement.AssemblyName, "suite");
            aggregator.ExcludeSidecarIfGenerationMatches(reportData.AssemblyName, "suite", staleGeneration);
            Directory.GetFiles(tempDirectory, $"*{ReportDataJson.SidecarExclusionExtension}").ShouldBeEmpty();
            aggregator.ReadAllSidecars().Single().MachineName.ShouldBe("replacement-machine");

            aggregator.ExcludeSidecar(reportData.AssemblyName, "suite");
            aggregator.ReadAllSidecars().ShouldBeEmpty();

            var latest = CreateReportData(reportData.AssemblyName, "latest-machine");
            aggregator.WriteSidecar(ReportDataJson.SerializeToBytes(latest), latest.AssemblyName, "suite");
            aggregator.ReadAllSidecars().Single().MachineName.ShouldBe("latest-machine");

            aggregator.IncludeSidecar(reportData.AssemblyName, "suite");
            aggregator.ReadAllSidecars().Single().MachineName.ShouldBe("latest-machine");
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
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            reporter.SetOutputPath(disabledHtmlPath);
            var disabledAssemblyName = reporter.BuildReportData().AssemblyName;

            TUnitSettings.Default.Reporting.JsonReportEnabled = true;
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(disabledAssemblyName), disabledHtmlPath, cancellationToken);
            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData("RemainingSuiteMarker"), remainingHtmlPath, cancellationToken);

            TUnitSettings.Default.Reporting.HtmlReportEnabled = false;
            await reporter.OnTestSessionFinishingAsync(null!);

            File.Exists(HtmlReporter.GetSidecarPath(disabledHtmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(2);
            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("RemainingSuiteMarker");
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
    public async Task Shared_Sidecar_Stays_Hidden_Until_Lock_Wait_Completes(CancellationToken cancellationToken)
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
            aggregationLock.ShouldNotBeNull();

            Task writeTask;
            using (aggregationLock!)
            {
                writeTask = reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancellationToken);

                File.Exists(HtmlReporter.GetSidecarPath(htmlPath)).ShouldBeTrue();
                Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
                Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarPublishingExtension}").Length.ShouldBe(1);
                aggregator.ReadAllSidecars().ShouldBeEmpty();
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

    [Test]
    public async Task Empty_Session_With_Disabled_Json_Removes_Stale_Sidecars(CancellationToken cancellationToken)
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
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            reporter.SetOutputPath(htmlPath);
            var assemblyName = reporter.BuildReportData().AssemblyName;

            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(assemblyName), htmlPath, cancellationToken);
            TUnitSettings.Default.Reporting.JsonReportEnabled = false;

            await reporter.OnTestSessionFinishingAsync(null!);

            File.Exists(HtmlReporter.GetSidecarPath(htmlPath)).ShouldBeFalse();
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
            aggregator.ReadAllSidecars().ShouldBeEmpty();
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
    public async Task Publication_Lock_File_Is_Stable_And_Reused(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", tempDirectory);

        try
        {
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            var reportData = CreateReportData();
            using var publicationLock = aggregator.BeginSidecarPublication(reportData.AssemblyName, "suite");
            var sidecarPath = aggregator.WriteSidecar(ReportDataJson.SerializeToBytes(reportData), reportData.AssemblyName, "suite");

            aggregator.ReadAllSidecars().ShouldBeEmpty();
            publicationLock.Dispose();

            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("Tests");
            File.Exists(sidecarPath + ReportDataJson.SidecarPublishingExtension).ShouldBeTrue();

            using (aggregator.BeginSidecarPublication(reportData.AssemblyName, "suite"))
            {
                aggregator.ReadAllSidecars().ShouldBeEmpty();
            }
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
    public async Task Cancelled_Lock_Wait_Keeps_Per_Suite_Summary(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            using var reporter = new HtmlReporter(new MockExtension());
            var githubReporter = new GitHubReporter(new MockExtension());
            reporter.SetGitHubReporter(githubReporter);
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            using var aggregationLock = await aggregator.AcquireLockAsync(cancellationToken);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var htmlPath = Path.Combine(tempDirectory, "suite-report.html");
            aggregator.ExcludeSidecar("Tests", htmlPath);

            await reporter.TryWriteSidecarAndAggregateAsync(
                CreateReportData(),
                htmlPath,
                cancelled.Token);

            githubReporter.SuppressPerSuiteSummary.ShouldBeFalse();
            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("Tests");
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExclusionExtension}").ShouldBeEmpty();
            File.Exists(Path.Combine(aggregationDirectory, ReportDataJson.MergedReportFileName)).ShouldBeFalse();
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
    public async Task Cancelled_Defer_Lock_Wait_Suppresses_Per_Suite_Summary(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "defer");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            using var reporter = new HtmlReporter(new MockExtension());
            var githubReporter = new GitHubReporter(new MockExtension());
            reporter.SetGitHubReporter(githubReporter);
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            using var aggregationLock = await aggregator.AcquireLockAsync(cancellationToken);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var htmlPath = Path.Combine(tempDirectory, "suite-report.html");

            await reporter.TryWriteSidecarAndAggregateAsync(
                CreateReportData(),
                htmlPath,
                cancelled.Token);

            githubReporter.SuppressPerSuiteSummary.ShouldBeTrue();
            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("Tests");
            File.Exists(Path.Combine(aggregationDirectory, ReportDataJson.MergedReportFileName)).ShouldBeFalse();
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
    [Timeout(30_000)]
    public async Task Disabled_Cleanup_Does_Not_Exclude_Active_Publication(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "suite-report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            using var reporter = new HtmlReporter(new MockExtension());
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            var reportData = CreateReportData();
            aggregator.WriteSidecar(ReportDataJson.SerializeToBytes(reportData), reportData.AssemblyName, htmlPath);
            TUnitSettings.Default.Reporting.JsonReportEnabled = false;

            using (aggregator.BeginSidecarPublication(reportData.AssemblyName, htmlPath))
            {
                var replacement = CreateReportData(reportData.AssemblyName, "active-publisher");
                aggregator.WriteSidecar(ReportDataJson.SerializeToBytes(replacement), replacement.AssemblyName, htmlPath);
                await reporter.TryWriteSidecarAndAggregateAsync(reportData, htmlPath, cancellationToken);
            }

            aggregator.ReadAllSidecars().Single().MachineName.ShouldBe("active-publisher");
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExclusionExtension}").ShouldBeEmpty();
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
    [Timeout(30_000)]
    public async Task Enabled_Publication_Contention_Preserves_Shared_Sidecar(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "suite-report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            using var reporter = new HtmlReporter(new MockExtension());
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            var reportData = CreateReportData("ContendedSuiteMarker");

            using (aggregator.BeginSidecarPublication(reportData.AssemblyName, htmlPath))
            {
                await reporter.TryWriteSidecarAndAggregateAsync(reportData, htmlPath, cancellationToken);

                Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExtension}").Length.ShouldBe(1);
                aggregator.ReadAllSidecars().ShouldBeEmpty();
            }

            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("ContendedSuiteMarker");
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
    [Timeout(30_000)]
    public async Task Enabled_Publication_Supersedes_In_Flight_Disabled_Cleanup(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "suite-report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var reporter = new HtmlReporter(new MockExtension());
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            var oldReport = CreateReportData(machineName: "old-machine");
            await reporter.TryWriteSidecarAndAggregateAsync(oldReport, htmlPath, cancellationToken);

            var aggregationLock = await aggregator.AcquireLockAsync(cancellationToken);
            aggregationLock.ShouldNotBeNull();
            Task cleanupTask;
            Task publicationTask;
            using (aggregationLock!)
            {
                TUnitSettings.Default.Reporting.JsonReportEnabled = false;
                cleanupTask = reporter.TryWriteSidecarAndAggregateAsync(oldReport, htmlPath, cancellationToken);
                Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExclusionExtension}").Length.ShouldBe(1);

                TUnitSettings.Default.Reporting.JsonReportEnabled = true;
                var newReport = CreateReportData(machineName: "new-machine");
                publicationTask = reporter.TryWriteSidecarAndAggregateAsync(newReport, htmlPath, cancellationToken);
            }

            await Task.WhenAll(cleanupTask, publicationTask);

            var publishedReport = aggregator.ReadAllSidecars().Single();
            publishedReport.MachineName.ShouldBe("new-machine");
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExclusionExtension}").ShouldBeEmpty();
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
    public async Task Enabled_Publication_Clears_Stale_Exclusion(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-test-{Guid.NewGuid():N}");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");
        var htmlPath = Path.Combine(tempDirectory, "suite-report.html");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true");
        Environment.SetEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory);

        try
        {
            using var reporter = new HtmlReporter(new MockExtension());
            var aggregator = ReportAggregator.TryCreateFromEnvironment(Environment.GetEnvironmentVariable)!;
            aggregator.ExcludeSidecar("Tests", htmlPath);

            await reporter.TryWriteSidecarAndAggregateAsync(CreateReportData(), htmlPath, cancellationToken);

            aggregator.ReadAllSidecars().Single().AssemblyName.ShouldBe("Tests");
            Directory.GetFiles(aggregationDirectory, $"*{ReportDataJson.SidecarExclusionExtension}").ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static ReportData CreateReportData(string assemblyName = "Tests", string machineName = "machine") => new()
    {
        AssemblyName = assemblyName,
        MachineName = machineName,
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
