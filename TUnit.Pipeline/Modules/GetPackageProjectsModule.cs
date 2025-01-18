using ModularPipelines.Context;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

public class GetPackageProjectsModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Get().ToList();
    }

    private IEnumerable<File> Get()
    {
        yield return Sourcy.DotNet.Projects.TUnit_Assertions;
        yield return Sourcy.DotNet.Projects.TUnit_Core;
        yield return Sourcy.DotNet.Projects.TUnit_Engine;
        yield return Sourcy.DotNet.Projects.TUnit;
        yield return Sourcy.DotNet.Projects.TUnit_Playwright;
        yield return Sourcy.DotNet.Projects.TUnit_Templates;
    }
}