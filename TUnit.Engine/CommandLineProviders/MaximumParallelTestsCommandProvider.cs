using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class MaximumParallelTestsCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string MaximumParallelTests = "maximum-parallel-tests";

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
            new CommandLineOption(MaximumParallelTests, "Maximum Parallel Tests", ArgumentArity.ExactlyOne, false)
        ];
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