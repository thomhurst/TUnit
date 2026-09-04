using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.CommandLineProviders;

internal class HtmlReporterCommandProvider(IExtension extension, HtmlCliMode htmlCliMode = HtmlCliMode.Default) : ICommandLineOptionsProvider
{
    public const string ReportHtml = "report-html";
    public const string ReportHtmlFilename = "report-html-filename";
    public const string TUnitReportHtmlFilename = "tunit-report-html-filename";

    public string ReportHtmlFilenameOption => htmlCliMode == HtmlCliMode.Namespaced
        ? TUnitReportHtmlFilename
        : ReportHtmlFilename;

    public Task<bool> IsEnabledAsync() => extension.IsEnabledAsync();

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    {
        if (htmlCliMode == HtmlCliMode.Namespaced)
        {
            return
            [
                new CommandLineOption(
                    TUnitReportHtmlFilename,
                    "Path for the HTML test report file (default: TestResults/{AssemblyName}-report.html)",
                    ArgumentArity.ExactlyOne,
                    false),
            ];
        }

        return
        [
            new CommandLineOption(
                ReportHtml,
                "Generate an HTML test report",
                ArgumentArity.Zero,
                false),
            new CommandLineOption(
                ReportHtmlFilename,
                "Path for the HTML test report file (default: TestResults/{AssemblyName}-report.html)",
                ArgumentArity.ExactlyOne,
                false),
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(
        CommandLineOption commandOption,
        string[] arguments)
    {
        if (commandOption.Name == ReportHtmlFilenameOption && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single output path must be provided for the HTML report");
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(
        ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}
