using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using TUnit.Pipeline.Modules.Tests;

namespace TUnit.Pipeline.Modules;

[DependsOn<GetPackageProjectsModule>]
[DependsOn<GenerateVersionModule>]
[DependsOnAllModulesInheritingFrom<TestModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        var versionResult = await GetModule<GenerateVersionModule>();

        var version = versionResult.Value!;

        var packageVersion = version.SemVer!;

        await projects.Value!.SelectAsync(
            async project => await context.DotNet()
                .Pack(
                    new DotNetPackOptions(project)
                    {
                        Properties =
                        [
                            new KeyValue("Version", version.SemVer!),
                            new KeyValue("PackageVersion", packageVersion!)
                        ],
                        NoBuild = true,
                        IncludeSource = true,
                        Configuration = Configuration.Release,
                    }, cancellationToken), cancellationToken: cancellationToken).ProcessOneAtATime();
        
        return projects.Value!.Select(x => new PackedProject(x.NameWithoutExtension, version.SemVer!)).ToList();
    }
}
