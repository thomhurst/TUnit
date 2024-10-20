using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

[DependsOn<PackTUnitFilesModule>]
public class MergeNuGetFilesModule : Module<List<File>>
{
    protected override Task<List<File>?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var gitDirectory = context.Git()
            .RootDirectory
            .AssertExists();
        
        var output = gitDirectory.CreateFolder("package-output");

        var packages = new List<File>();
        
        foreach (var grouping in gitDirectory
                     .GetFiles(x => x.Extension == ".nupkg")
                     .GroupBy(x => x.Name))
        {
            var packageFolder = output.CreateFolder(grouping.Key);

            foreach (var file in grouping)
            {
                context.Zip.UnZipToFolder(file, packageFolder);
                file.Delete();
            }

            var nugetPackage = output.GetFile($"{grouping.Key}.nupkg");
            
            context.Zip.ZipFolder(packageFolder, nugetPackage);
            
            packages.Add(nugetPackage);
        }

        foreach (var file in gitDirectory
                     .GetFiles(x => x.Extension == ".snupkg")
                     .DistinctBy(x => x.Name))
        {
            file.MoveTo(output);
            
            foreach (var fileToDelete in gitDirectory.GetFiles(x => x.Name == file.Name).Except([file]))
            {
                fileToDelete.Delete();
            }
        }
        
        return Task.FromResult<List<File>?>(packages);
    }
}
