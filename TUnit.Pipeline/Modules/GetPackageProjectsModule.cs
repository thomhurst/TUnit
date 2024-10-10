using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

public class GetPackageProjectsModule : Module<List<File>>
{
    protected override Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return context.Git().RootDirectory
            .GetFiles(x => x.Extension == ".csproj")
            .Where(x => !x.Name.Contains("Pipeline"))
            .Where(x => !x.Name.Contains("Analyzer"))
            .Where(x => !x.Name.Contains("Generator"))
            .Where(x => !x.Name.Contains("Sample"))
            .Where(x => !x.Name.Contains("TestProject"))
            .Where(x => !x.Name.Contains("Test"))
            .Where(x => !x.Name.Contains("Timer"))
            .ToList()
            .AsTask<List<File>?>();
    }
}