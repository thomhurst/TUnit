using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;
[DependsOn<CreateLocalNuGetDirectoryModule>]
[DependsOn<MoveNuGetPackagesToLocalSourceModule>]
[DependsOn<PackTUnitFilesModule>]
public class AddReferencesToTestProject : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var testProject = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();
        var projects = await GetModule<PackTUnitFilesModule>();
        var localNugetDirectory = await GetModule<CreateLocalNuGetDirectoryModule>();

        await projects.Value!
            .ForEachAsync(
                x => context.DotNet().Remove.Package(new DotNetRemovePackageOptions(x.Name), cancellationToken),
                cancellationToken: cancellationToken)
            .ProcessOneAtATime();
        
        return await projects.Value!
            .SelectAsync(async x => await context.DotNet().Add.Package(new DotNetAddPackageOptions(testProject, x.Name)
            {
                Version = x.Version,
                Source = localNugetDirectory.Value!
            }, cancellationToken), cancellationToken: cancellationToken).ProcessOneAtATime();
    }
}