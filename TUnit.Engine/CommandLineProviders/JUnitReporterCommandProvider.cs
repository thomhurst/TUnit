using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class JUnitReporterCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string JUnitOutputPathOption = "junit-output-path";

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
                JUnitOutputPathOption,
                "Path to output JUnit XML file (default: TestResults/{AssemblyName}-junit.xml)",
                ArgumentArity.ExactlyOne,
                false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(
        CommandLineOption commandOption,
        string[] arguments)
    {
        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(
        ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}
