using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules.Tests;

[DependsOn<BuildTestProjectModule>]
public class ListTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();
        
        return await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            NoBuild = true,
            Arguments = [ "--list-tests" ]
        }, cancellationToken);

    }
}