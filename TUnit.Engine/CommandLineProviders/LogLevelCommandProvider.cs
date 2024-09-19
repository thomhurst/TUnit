using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using TUnit.Core;

namespace TUnit.Engine.CommandLineProviders;

internal class LogLevelCommandProvider : ICommandLineOptionsProvider
{
    public const string LogLevel = "tunit-log-level";
    
    private readonly IExtension _extension;

    public LogLevelCommandProvider(IExtension extension)
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
            new CommandLineOption(LogLevel, "Set the log level for TUnit loggers", ArgumentArity.ExactlyOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == LogLevel && Enum.TryParse<LogLevel>(arguments[0], true, out var logLevel))
        {
            GlobalContext.LogLevel = logLevel;
            return ValidationResult.ValidTask;
        }

        return ValidationResult.InvalidTask($"Value must be one of: {string.Join(", ", Enum.GetNames<LogLevel>())}");
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}