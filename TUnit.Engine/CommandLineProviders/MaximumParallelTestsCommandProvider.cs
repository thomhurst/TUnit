using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine;

public class MaximumParallelTestsCommandProvider : ICommandLineOptionsProvider
{
    public const string MaximumParallelTests = "maximum-parallel-tests";
    
    private readonly IExtension _extension;

    public MaximumParallelTestsCommandProvider(IExtension extension)
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
            new CommandLineOption(MaximumParallelTests, "Maximum Parallel Tests", ArgumentArity.ExactlyOne, false),
        };
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == MaximumParallelTests && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single number must be provided for maximum parallel tests");
        }
        
        if (commandOption.Name == MaximumParallelTests && (!int.TryParse(arguments[0], out var maximumParallelTests) || maximumParallelTests < 1))
        {
            return ValidationResult.InvalidTask("Maximum parallel tests value was not a positive integer");
        }
        
        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}