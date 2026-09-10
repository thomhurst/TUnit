using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

public class AddLocalNuGetRepositoryModule : Module<Folder>
{
    protected override async Task<Folder?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = new Folder(Path.Combine(localAppData, "LocalNuget"));
        folder.Create();

        await context.DotNet().Nuget.Add.Source(new DotNetNugetAddSourceOptions
        {
            Name = "LocalNuget",
            Packagesourcepath = folder.Path,
        }, new CommandExecutionOptions
        {
            LogSettings = new CommandLoggingOptions
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
