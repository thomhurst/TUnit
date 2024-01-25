using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[DependsOn<CreateLocalNuGetDirectoryModule>]
[DependsOn<PackTUnitFilesModule>]
public class MoveNuGetPackagesToLocalSourceModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var localNugetDirectory = await GetModule<CreateLocalNuGetDirectoryModule>();
        
        foreach (var file in localNugetDirectory.Value!.ListFiles().Where(x => x.Name.Contains("TUnit")))
        {
            file.Delete();
        }
        
        var nugetPackages = context.Git().RootDirectory
            .GetFiles(x => x.Extension is ".nupkg" or ".snupkg");

        return nugetPackages
            .Select(x => x.MoveTo(localNugetDirectory.Value.AssertExists()))
            .ToList();
    }
}