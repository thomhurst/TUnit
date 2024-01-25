using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<CreateLocalNuGetDirectoryModule>]
public class AddLocalNuGetDirectoryModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var directoryResult = await GetModule<CreateLocalNuGetDirectoryModule>();

        return await context.DotNet().Nuget.Add
            .Source(new DotNetNugetAddSourceOptions(directoryResult.Value!), cancellationToken);
    }
}