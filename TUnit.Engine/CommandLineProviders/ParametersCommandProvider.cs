using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class ParametersCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string TestParameter = "test-parameter";

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;

    public string Version => extension.Version;

    public string DisplayName => extension.DisplayName;

    public string Description => extension.Description;

    public readonly Regex Regex = new("^.+=.*$");
    
    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    {
        return
        [
            new CommandLineOption(TestParameter, "Custom parameters to pass to TUnit", ArgumentArity.OneOrMore, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (arguments.Any(argument => !Regex.IsMatch(argument)))
        {
            return ValidationResult.InvalidTask("TestParameter must be in the format of KEY=VALUE");
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}