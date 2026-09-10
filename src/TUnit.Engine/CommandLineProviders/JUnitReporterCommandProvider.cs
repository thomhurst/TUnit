using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using TUnit.Engine.Helpers;

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
        if (commandOption.Name == JUnitOutputPathOption && arguments.Length == 1)
        {
            try
            {
                PathValidator.ValidateAndNormalizePath(arguments[0], JUnitOutputPathOption);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(ValidationResult.Invalid(ex.Message));
            }
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(
        ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}
