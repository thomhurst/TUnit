using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class ParametersCommandProvider : ICommandLineOptionsProvider
{
    public const string TestParameter = "test-parameter";
    
    private readonly IExtension _extension;

    public ParametersCommandProvider(IExtension extension)
    {
        _extension = extension;
    }

    public Task<bool> IsEnabledAsync()
    {
        return _extension.IsEnabledAsync();
    }

    public string Uid => _extension.Uid;

    public string Version => _extension.Version;

    public string DisplayName => _extension.DisplayName;

    public string Description => _extension.Description;

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