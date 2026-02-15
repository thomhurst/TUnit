using ModularPipelines.Context;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;

public class GetPackageProjectsModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return
        [
            Sourcy.DotNet.Projects.TUnit_Assertions,
            Sourcy.DotNet.Projects.TUnit_Assertions_FSharp,
            Sourcy.DotNet.Projects.TUnit_Core,
            Sourcy.DotNet.Projects.TUnit_Engine,
            Sourcy.DotNet.Projects.TUnit,
            Sourcy.DotNet.Projects.TUnit_Playwright,
            Sourcy.DotNet.Projects.TUnit_Templates,
            Sourcy.DotNet.Projects.TUnit_Logging_Microsoft,
            Sourcy.DotNet.Projects.TUnit_AspNetCore,
            Sourcy.DotNet.Projects.TUnit_FsCheck
        ];
    }
}
