using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Enums;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<CopyToLocalNuGetModule>]
[DependsOn<GenerateVersionModule>]
public class TestTemplatePackageModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var version = await GetModule<GenerateVersionModule>();

        await context.DotNet().New(new DotNetNewOptions("uninstall")
        {
            Arguments = ["TUnit.Templates"],
            ThrowOnNonZeroExitCode = false,
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);

        await context.DotNet().New(new DotNetNewOptions("install")
        {
            Arguments = [$"TUnit.Templates::{version.Value!.SemVer}"],
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);

        await context.DotNet().New(new DotNetNewOptions("TUnit")
        {
            Name = "MyTestProject",
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);

        await context.DotNet().New(new DotNetNewOptions("TUnit.AspNet")
        {
            Name = "MyTestProject2",
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);

        return await context.DotNet().New(new DotNetNewOptions("TUnit.Playwright")
        {
            Name = "MyTestProject3",
            CommandLogging = CommandLogging.Input | CommandLogging.Error | CommandLogging.Duration | CommandLogging.ExitCode
        }, cancellationToken);
    }
}
