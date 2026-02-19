using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class JsonLogReporterCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string ReportJsonLog = "report-json-log";
    public const string ReportJsonLogFilename = "report-json-log-filename";

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
                ReportJsonLog,
                "Enable structured JSON log output for test events",
                ArgumentArity.Zero,
                false),
            new CommandLineOption(
                ReportJsonLogFilename,
                "Path for structured JSON log output file (default: TestResults/{AssemblyName}-log.jsonl)",
                ArgumentArity.ExactlyOne,
                false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(
        CommandLineOption commandOption,
        string[] arguments)
    {
        if (commandOption.Name == ReportJsonLogFilename && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single filename must be specified for --report-json-log-filename");
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(
        ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}
