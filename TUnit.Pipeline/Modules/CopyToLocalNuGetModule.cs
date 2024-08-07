using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules;
[DependsOn<PackTUnitFilesModule>]
[DependsOn<AddLocalNuGetRepositoryModule>]
public class CopyToLocalNuGetModule : Module<List<File>>
{
    protected override async Task<List<File>?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var folder = await GetModule<AddLocalNuGetRepositoryModule>();
        return context.Git().RootDirectory.GetFiles(x => x.Extension.EndsWith("nupkg")).Select(x => x.CopyTo(folder.Value!)).ToList();
    }
}

[DependsOn<GenerateVersionModule>]
[DependsOn<CopyToLocalNuGetModule>]
public class TestNugetPackageModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var version = await GetModule<GenerateVersionModule>();

        var project = context.Git()
            .RootDirectory
            .AssertExists()
            .FindFile(x => x.Name == "TUnit.NugetTester.csproj")
            .AssertExists();

        return await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            Properties =
            [
                new KeyValue("TUnitVersion", version.Value!.SemVer!)
            ]
        }, cancellationToken);
    }
}