using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<AddReferencesToTestProject>]
[DependsOn<GetPackageProjectsModule>]
public class BuildProjectsModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        return await projects.Value!.SelectAsync(x => context.DotNet().Build(new DotNetBuildOptions(x), cancellationToken), cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}