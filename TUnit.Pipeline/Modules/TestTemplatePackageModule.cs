using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<CopyToLocalNuGetModule>]
public class TestTemplatePackageModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        await context.DotNet().New(new DotNetNewOptions("uninstall")
        {
            Arguments = ["TUnit.Templates"],
            ThrowOnNonZeroExitCode = false
        }, cancellationToken);
        
        await context.DotNet().New(new DotNetNewOptions("install")
        {
            Arguments = ["TUnit.Templates"]
        }, cancellationToken);
        
        await context.DotNet().New(new DotNetNewOptions("TUnit")
        {
            Name = "MyTestProject"
        }, cancellationToken);
        
        await context.DotNet().New(new DotNetNewOptions("TUnit.AspNet")
        {
            Name = "MyTestProject"
        }, cancellationToken);
        
        return await context.DotNet().New(new DotNetNewOptions("TUnit.Playwright")
        {
            Name = "MyTestProject"
        }, cancellationToken);
    }
}