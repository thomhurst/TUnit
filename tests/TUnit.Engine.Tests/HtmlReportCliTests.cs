using CliWrap;
using CliWrap.Buffered;
using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class HtmlReportCliTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Combined_Html_Report_Packages_Accept_TUnit_Namespaced_Option()
    {
        await RunTestsWithFilter(
            "/*/*/BasicTests/SynchronousTest",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
            ],
            new RunOptions()
                .WithArgument("--tunit-report-html-filename")
                .WithArgument("tunit-report.html"));
    }
}

public class DefaultHtmlReportCliTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TUnit_Only_Project_Accepts_Legacy_Options(bool reflection)
    {
        var tempDirectory = CreateTempDirectory();
        var reportPath = Path.Combine(tempDirectory, "custom-report.html");

        try
        {
            var result = await RunTUnitOnlyProject(
                tempDirectory,
                reflection,
                "--report-html",
                "--report-html-filename",
                reportPath);

            AssertSuccessful(result);
            File.Exists(reportPath).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TUnit_Only_Project_Uses_Default_Output_Path(bool reflection)
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var result = await RunTUnitOnlyProject(tempDirectory, reflection);

            AssertSuccessful(result);

            var reports = Directory.GetFiles(tempDirectory, "*-report.html");
            reports.Length.ShouldBe(1);
            Path.GetFileName(reports[0]).ShouldStartWith("TUnit.TestProject.HtmlReportDefaults-");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tunit-html-defaults-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static Task<BufferedCommandResult> RunTUnitOnlyProject(
        string resultsDirectory,
        bool reflection,
        params string[] htmlArguments)
    {
        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject_HtmlReportDefaults;
        List<string> arguments =
        [
            "run",
            "--no-build",
            "--project", testProject.FullName,
            "--framework", "net10.0",
            "--configuration", "Release",
            "--",
            "--treenode-filter", "/*/*/DefaultHtmlReportTests/Pass",
            "--results-directory", resultsDirectory,
            ..htmlArguments,
        ];

        if (reflection)
        {
            arguments.Add("--reflection");
        }

        return Cli.Wrap("dotnet")
            .WithArguments(arguments)
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithEnvironmentVariables(new Dictionary<string, string?>
            {
                ["TUNIT_DISABLE_HTML_REPORTER"] = "false",
                ["TUNIT_DISABLE_JSON_REPORT"] = "true",
                ["TUNIT_DISABLE_ARTIFACT_UPLOAD"] = "true",
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
    }

    private static void AssertSuccessful(BufferedCommandResult result)
    {
        result.ExitCode.ShouldBe(0, $"""
                                    Standard output:
                                    {result.StandardOutput}

                                    Standard error:
                                    {result.StandardError}
                                    """);
    }
}
