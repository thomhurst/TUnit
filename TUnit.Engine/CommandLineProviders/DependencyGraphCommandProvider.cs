using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class DependencyGraphCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string ExportDependencyGraph = "export-dependency-graph";

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
            new CommandLineOption(ExportDependencyGraph, "Export test dependency graph as a Mermaid (.mmd) file. Optionally specify an output file path.", ArgumentArity.ZeroOrOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == ExportDependencyGraph && arguments.Length > 1)
        {
            return ValidationResult.InvalidTask("At most one output file path can be specified for --export-dependency-graph");
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}
