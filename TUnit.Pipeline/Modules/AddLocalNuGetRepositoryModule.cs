using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Modules;
using TUnit.Pipeline.Extensions;

namespace TUnit.Pipeline.Modules;

public class AddLocalNuGetRepositoryModule : Module<Folder>
{
    protected override async Task<Folder?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var folder = context.FileSystem.GetFolder(Environment.SpecialFolder.LocalApplicationData).GetFolder("LocalNuget").Create();
        await context.DotNet().NugetAddSourceQuiet(new DotNetNugetAddSourceOptions(folder), cancellationToken);
        return folder;
    }
}
