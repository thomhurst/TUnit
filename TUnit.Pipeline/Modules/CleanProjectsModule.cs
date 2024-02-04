using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Attributes;

namespace TUnit.Pipeline.Modules;
[DependsOn<GetPackageProjectsModule>]
public class CleanProjectsModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        return await projects.Value!.SelectAsync(x => context.DotNet().Clean(new DotNetCleanOptions(x), cancellationToken), cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}