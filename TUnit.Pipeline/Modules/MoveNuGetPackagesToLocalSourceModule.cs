using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[DependsOn<CreateLocalNuGetDirectoryModule>]
public class MoveNuGetPackagesToLocalSourceModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var localNugetDirectory = await GetModule<CreateLocalNuGetDirectoryModule>();
        
        var nugetPackages = context.Git().RootDirectory
            .GetFiles(x => x.Extension is ".nupkg" or ".snupkg");

        return nugetPackages
            .Select(x => x.MoveTo(localNugetDirectory.Value.AssertExists()))
            .ToList();
    }
}