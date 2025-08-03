using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using TUnit.Engine.Logging;

namespace TUnit.Engine.CommandLineProviders;

internal class VerbosityCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string Verbosity = "verbosity";

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
            new CommandLineOption(Verbosity, "Output verbosity level: minimal, normal, verbose, debug (default: normal)", ArgumentArity.ExactlyOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == Verbosity && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single verbosity level must be provided: minimal, normal, verbose, or debug");
        }

        if (commandOption.Name == Verbosity)
        {
            var verbosityArg = arguments[0].ToLowerInvariant();
            if (!IsValidVerbosity(verbosityArg))
            {
                return ValidationResult.InvalidTask($"Invalid verbosity level '{arguments[0]}'. Valid options: minimal, normal, verbose, debug");
            }
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }

    private static bool IsValidVerbosity(string verbosity)
    {
        return verbosity is "minimal" or "normal" or "verbose" or "debug";
    }

    /// <summary>
    /// Parses verbosity from command line arguments
    /// </summary>
    public static TUnitVerbosity ParseVerbosity(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return TUnitVerbosity.Normal;
        }

        return arguments[0].ToLowerInvariant() switch
        {
            "minimal" => TUnitVerbosity.Minimal,
            "normal" => TUnitVerbosity.Normal,
            "verbose" => TUnitVerbosity.Verbose,
            "debug" => TUnitVerbosity.Debug,
            _ => TUnitVerbosity.Normal
        };
    }
}