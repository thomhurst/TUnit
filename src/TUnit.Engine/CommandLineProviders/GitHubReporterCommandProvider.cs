using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using TUnit.Engine.Reporters;

namespace TUnit.Engine.CommandLineProviders;

internal class GitHubReporterCommandProvider(IExtension extension) : ICommandLineOptionsProvider
{
    public const string GitHubReporterStyleOption = "github-reporter-style";

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
            new CommandLineOption(GitHubReporterStyleOption, "GitHub reporter output style: collapsible (default) or full", ArgumentArity.ExactlyOne, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        if (commandOption.Name == GitHubReporterStyleOption && arguments.Length != 1)
        {
            return ValidationResult.InvalidTask("A single reporter style must be provided: collapsible or full");
        }

        if (commandOption.Name == GitHubReporterStyleOption)
        {
            var style = arguments[0].ToLowerInvariant();
            if (!IsValidReporterStyle(style))
            {
                return ValidationResult.InvalidTask($"Invalid reporter style '{arguments[0]}'. Valid options: collapsible, full");
            }
        }

        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }

    private static bool IsValidReporterStyle(string style)
    {
        return style is "collapsible" or "full";
    }

    public static GitHubReporterStyle ParseReporterStyle(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return GitHubReporterStyle.Collapsible;
        }

        return arguments[0].ToLowerInvariant() switch
        {
            "collapsible" => GitHubReporterStyle.Collapsible,
            "full" => GitHubReporterStyle.Full,
            _ => GitHubReporterStyle.Collapsible
        };
    }
}