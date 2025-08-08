using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using TUnit.Pipeline.Extensions;

namespace TUnit.Pipeline.Modules;

[DependsOn<CopyToLocalNuGetModule>]
[DependsOn<GenerateVersionModule>]
public class TestTemplatePackageModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var version = await GetModule<GenerateVersionModule>();

        await context.DotNet().NewQuiet(new DotNetNewOptions("uninstall")
        {
            Arguments = ["TUnit.Templates"],
            ThrowOnNonZeroExitCode = false
        }, cancellationToken);

        await context.DotNet().NewQuiet(new DotNetNewOptions("install")
        {
            Arguments = [$"TUnit.Templates::{version.Value!.SemVer}"]
        }, cancellationToken);

        await context.DotNet().NewQuiet(new DotNetNewOptions("TUnit")
        {
            Name = "MyTestProject"
        }, cancellationToken);

        await context.DotNet().NewQuiet(new DotNetNewOptions("TUnit.AspNet")
        {
            Name = "MyTestProject2"
        }, cancellationToken);

        return await context.DotNet().NewQuiet(new DotNetNewOptions("TUnit.Playwright")
        {
            Name = "MyTestProject3"
        }, cancellationToken);
    }
}
