using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class FailFastCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string FailFast = "fail-fast";

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;
    
    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    {
        return
        [
            new CommandLineOption(FailFast, "Cancel the test run after the first test failure", ArgumentArity.Zero, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}