using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.SpeedComparison.Pipeline;

[NotInParallel("SpeedComparison")]
[DependsOn<FindProjectsModule>]
public class MSTestModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projectPaths = await GetModule<FindProjectsModule>();
        var project = projectPaths.Value!.MSTest;

        return await context.DotNet().Test(new DotNetTestOptions(project)
        {
            Configuration = Configuration.Release,
            NoBuild = true
        }, cancellationToken);
    }
}