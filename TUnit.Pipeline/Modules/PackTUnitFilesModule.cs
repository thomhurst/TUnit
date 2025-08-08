using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[DependsOn<GetPackageProjectsModule>]
[DependsOn<GenerateVersionModule>]
[DependsOn<RunEngineTestsModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    protected override async Task<List<PackedProject>?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var projects = await GetModule<GetPackageProjectsModule>();
        var versionResult = await GetModule<GenerateVersionModule>();

        var version = versionResult.Value!;

        var packageVersion = version.SemVer!;

        var packedProjects = new List<PackedProject>();

        foreach (var project in projects.Value!)
        {
            await context.DotNet()
                .Pack(
                    new DotNetPackOptions(project)
                    {
                        Properties =
                        [
                            new KeyValue("Version", version.SemVer!),
                            new KeyValue("PackageVersion", packageVersion),
                            new KeyValue("AssemblyFileVersion", version.SemVer!),
                            new KeyValue("IsPackTarget", "true")
                        ],
                        IncludeSource = project == Sourcy.DotNet.Projects.TUnit_Templates ? false : true,
                        Configuration = Configuration.Release,
                    }, cancellationToken);

            packedProjects.Add(new PackedProject(project.NameWithoutExtension, version.SemVer!));
        }

        return packedProjects;
    }
}
