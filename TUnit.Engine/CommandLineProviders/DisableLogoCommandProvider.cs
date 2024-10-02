using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine.CommandLineProviders;

internal class DisableLogoCommandProvider : ICommandLineOptionsProvider
{
    public const string DisableLogo = "disable-logo";
    
    private readonly IExtension _extension;

    public DisableLogoCommandProvider(IExtension extension)
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
            new CommandLineOption(DisableLogo, "Disables the TUnit logo when starting a test session", ArgumentArity.Zero, false)
        ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        return ValidationResult.ValidTask;
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        return ValidationResult.ValidTask;
    }
}