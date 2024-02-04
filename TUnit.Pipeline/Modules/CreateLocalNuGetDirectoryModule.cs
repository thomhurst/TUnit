using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.FileSystem;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

public class CreateLocalNuGetDirectoryModule : Module<Folder>
{
    protected override Task<Folder?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return context.FileSystem.GetFolder(Environment.SpecialFolder.UserProfile)
            .GetFolder("LocalNuGet")
            .Create()
            .AsTask<Folder?>();
    }
}