using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class JsonOutputCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string OutputJson = "output-json";
    public const string OutputJsonFilename = "output-json-filename";
    public const string OutputJsonFilenamePrefix = "output-json-prefix";

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
            new CommandLineOption(OutputJson, "Output JSON", ArgumentArity.Zero, false),
            new CommandLineOption(OutputJsonFilename, "Output JSON filename", ArgumentArity.ExactlyOne, false),
            new CommandLineOption(OutputJsonFilenamePrefix, "Output JSON filename prefix", ArgumentArity.ExactlyOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == OutputJsonFilenamePrefix && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("Invalid number of output json filename specified");
        }
        
        if (commandOption.Name == OutputJsonFilename && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("Invalid number of output json filename specified");
        }
        
        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}