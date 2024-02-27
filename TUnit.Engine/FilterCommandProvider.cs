using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine;

public class FilterCommandProvider : ICommandLineOptionsProvider
{
    public const string Filter = "filter";
    
    private readonly IExtension _extension;

    public FilterCommandProvider(IExtension extension)
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
    
    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    {
        return new[]
        {
            new CommandLineOption(Filter, "Basic filter", ArgumentArity.ZeroOrOne, false)
        };
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == Filter && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask($"Invalid arguments count of {arguments.Length}");
        }

        // No problem found
        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}