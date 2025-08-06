using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class ParallelismStrategyCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string ParallelismStrategy = "parallelism-strategy";

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
            new CommandLineOption(ParallelismStrategy, "Parallelism strategy: fixed or adaptive (default: adaptive)", ArgumentArity.ExactlyOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == ParallelismStrategy && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single value must be provided for parallelism strategy");
        }

        if (commandOption.Name == ParallelismStrategy)
        {
            var strategy = arguments[0].ToLowerInvariant();
            if (strategy != "fixed" && strategy != "adaptive")
            {
                return ValidationResult.InvalidTask("Parallelism strategy must be 'fixed' or 'adaptive'");
            }
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}