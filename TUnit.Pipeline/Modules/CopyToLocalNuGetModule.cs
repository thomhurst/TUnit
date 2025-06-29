using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;
[DependsOn<PackTUnitFilesModule>]
[DependsOn<AddLocalNuGetRepositoryModule>]
public class CopyToLocalNuGetModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var folder = await GetModule<AddLocalNuGetRepositoryModule>();
        return context.Git().RootDirectory.GetFiles(x => x.Extension.EndsWith("nupkg")).Select(x => x.CopyTo(folder.Value!)).ToList();
    }
}
