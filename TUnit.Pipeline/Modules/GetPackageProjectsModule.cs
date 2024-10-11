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
            .Where(x => !x.Name.Contains("Pipeline", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Name.Contains("Analyzer", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Name.Contains("Generator", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Name.Contains("Sample", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Name.Contains("Timer", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Name.Contains("CodeFix", StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsTask<List<File>?>();
    }
}