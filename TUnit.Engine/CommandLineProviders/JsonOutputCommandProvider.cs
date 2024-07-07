using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class JsonOutputCommandProvider : ICommandLineOptionsProvider
{
    public const string OutputJson = "output-json";
    public const string OutputJsonFilename = "output-json-filename";
    public const string OutputJsonFilenamePrefix = "output-json-prefix";
    
    private readonly IExtension _extension;

    public JsonOutputCommandProvider(IExtension extension)
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