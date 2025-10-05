using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using LogLevel = TUnit.Core.Logging.LogLevel;

namespace TUnit.Engine.CommandLineProviders;

internal class LogLevelCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string LogLevelOption = "log-level";

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
            new CommandLineOption(LogLevelOption, "Minimum log level for test output: Trace, Debug, Information, Warning, Error, Critical, None (default: Information)", ArgumentArity.ExactlyOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == LogLevelOption && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single log level must be provided: Trace, Debug, Information, Warning, Error, Critical, or None");
        }

        if (commandOption.Name == LogLevelOption)
        {
            var logLevelArg = arguments[0];
            if (!IsValidLogLevel(logLevelArg))
            {
                return ValidationResult.InvalidTask($"Invalid log level '{arguments[0]}'. Valid options: Trace, Debug, Information, Warning, Error, Critical, None");
            }
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }

    private static bool IsValidLogLevel(string logLevel)
    {
        return Enum.TryParse<LogLevel>(logLevel, ignoreCase: true, out _);
    }

    /// <summary>
    /// Parses log level from command line arguments
    /// </summary>
    public static LogLevel ParseLogLevel(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return LogLevel.Information;
        }

        if (Enum.TryParse<LogLevel>(arguments[0], ignoreCase: true, out var result))
        {
            return result;
        }

        return LogLevel.Information;
    }
}
