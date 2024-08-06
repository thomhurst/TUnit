using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.SpeedComparison.Pipeline;

public class FindProjectsModule : Module<ProjectPaths>
{
    protected override async Task<ProjectPaths?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new ProjectPaths
        {
            TUnit = Find(context, "TUnitTimer.csproj"), 
            xUnit = Find(context, "xUnitTimer.csproj"),
            NUnit = Find(context, "NUnitTimer.csproj"), 
            MSTest = Find(context, "MSTestTimer.csproj"),
        };
    }

    private File Find(IPipelineContext context, string fileName)
    {
        return context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == fileName)
            .AssertExists();
    }
}