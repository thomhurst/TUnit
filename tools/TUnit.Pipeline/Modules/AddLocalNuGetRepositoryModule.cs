using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.Packaging)]
public class AddLocalNuGetRepositoryModule : Module<FolderPath>
{
    protected override async Task<FolderPath> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = new FolderPath(Path.Combine(localAppData, "LocalNuget"));
        folder.Create();

        await context.Tools.DotNet.NuGet.Add.SourceAsync(new DotNetNuGetAddSourceOptions(folder.Path)
        {
            Name = "LocalNuget",
        }, new CommandExecutionOptions
        {
            Logging = new CommandLoggingOptions
            {
                ShowCommandArguments = true,
                ShowStandardError = true,
                ShowExecutionTime = true,
                ShowExitCode = true
            }
        }, cancellationToken);
        return folder;
    }
}
