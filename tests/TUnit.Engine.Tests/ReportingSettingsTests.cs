using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ReportingSettingsTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Discovery_Hook_Can_Disable_Reporting(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"tunit-report-settings-{Guid.NewGuid():N}");
        var reportPath = Path.Combine(tempDirectory, "report.html");
        var aggregationDirectory = Path.Combine(tempDirectory, "aggregate");

        try
        {
            var options = new RunOptions()
                .WithArgument("--tunit-report-html-filename")
                .WithArgument(reportPath)
                .WithEnvironmentVariable("TUNIT_DISABLE_HTML_REPORTER", "false")
                .WithEnvironmentVariable("TUNIT_DISABLE_JSON_REPORT", "false")
                .WithEnvironmentVariable("TUNIT_DISABLE_ARTIFACT_UPLOAD", "false")
                .WithEnvironmentVariable("TUNIT_AGGREGATE_REPORTS", "true")
                .WithEnvironmentVariable("TUNIT_AGGREGATE_DIR", aggregationDirectory)
                .WithEnvironmentVariable("TUNIT_TEST_DISABLE_REPORTING_FROM_DISCOVERY_HOOK", "true")
                .WithGracefulCancellationToken(cancellationToken);

            await RunTestsWithFilter(
                "/*/*/ReportingSettingsTests/*",
                [
                    result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                    _ => File.Exists(reportPath).ShouldBeFalse(),
                    _ => Directory.Exists(aggregationDirectory).ShouldBeFalse(),
                ],
                options);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
