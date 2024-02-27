using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[DependsOn<PackTUnitFilesModule>]
public class CopyToLocalNuGetModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        
        return context.Git()
            .RootDirectory
            .GetFiles(x => x.Extension.EndsWith("nupkg"))
            .Select(x => x.CopyTo(context.Git().RootDirectory.Root.GetFolder("LocalNuget")))
            .ToList();
    }
}