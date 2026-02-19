using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class HtmlReporterCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string ReportHtml = "report-html";
    public const string ReportHtmlFilename = "report-html-filename";

    public Task<bool> IsEnabledAsync() => extension.IsEnabledAsync();

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    {
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
                false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(
        CommandLineOption commandOption,
        string[] arguments)
    {
        if (commandOption.Name == ReportHtmlFilename && arguments.Length != 1)
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
