using ModularPipelines;
using ModularPipelines.Context;
using File = ModularPipelines.FileSystem.FilePath;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.Packaging)]
[DependsOn<PackTUnitFilesModule>]
[DependsOn<AddLocalNuGetRepositoryModule>]
public class CopyToLocalNuGetModule : Module<List<File>>
{
    protected override async Task<List<File>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var folder = await context.GetModule<AddLocalNuGetRepositoryModule>();
        return (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.GetFiles(x => x.Name.StartsWith("TUnit", StringComparison.InvariantCultureIgnoreCase) && x.Extension.EndsWith("nupkg"))
            .Select(x => x.CopyTo(folder.ValueOrDefault!))
            .ToList();
    }
}
