using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[DependsOn<CopyToLocalNuGetModule>]
[DependsOn<GenerateVersionModule>]
public class TestTemplatePackageModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context,
        CancellationToken cancellationToken)
    {
        var version = await context.GetModule<GenerateVersionModule>();

        var logSettings = new CommandLoggingOptions
        {
            ShowCommandArguments = true,
            ShowStandardError = true,
            ShowExecutionTime = true,
            ShowExitCode = true
        };

        // Uninstall existing template
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "uninstall",
            TemplateArgs = "TUnit.Templates",
        }, new CommandExecutionOptions
        {
            ThrowOnNonZeroExitCode = false,
            LogSettings = logSettings
        }, cancellationToken);

        // Install template with specific version
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "install",
            TemplateArgs = $"TUnit.Templates::{version.ValueOrDefault!.SemVer}",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);

        // Create TUnit project
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "TUnit",
            Name = "MyTestProject",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);

        // Create TUnit project with the opt-in dotCover parameter
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "TUnit",
            Name = "MyTestProjectDotCover",
            TemplateArgs = "--enable-dotcover",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);

        // Create TUnit.FSharp project with the opt-in dotCover parameter
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "TUnit.FSharp",
            Name = "MyTestProjectFSharpDotCover",
            TemplateArgs = "--enable-dotcover",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);

        // Create TUnit.VB project with the opt-in dotCover parameter
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "TUnit.VB",
            Name = "MyTestProjectVBDotCover",
            TemplateArgs = "--enable-dotcover",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);

        // Create TUnit.AspNet project
        await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "TUnit.AspNet",
            Name = "MyTestProject2",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);

        // Create TUnit.Playwright project
        return await context.DotNet().New.Execute(new DotNetNewOptions
        {
            TemplateShortName = "TUnit.Playwright",
            Name = "MyTestProject3",
        }, new CommandExecutionOptions
        {
            LogSettings = logSettings
        }, cancellationToken);
    }
}
