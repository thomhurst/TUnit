using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

[DependsOn<GetPackageProjectsModule>]
[DependsOn<GenerateVersionModule>]
[DependsOn<RunEngineTestsModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    // Packages in beta get a "-beta" suffix appended to their version.
    // Remove entries from this set once a package is considered stable.
    private static readonly HashSet<string> BetaPackages =
    [
        "TUnit.Mock"
    ];

    protected override async Task<List<PackedProject>?> ExecuteAsync(IModuleContext context,
        CancellationToken cancellationToken)
    {
        var projects = await context.GetModule<GetPackageProjectsModule>();
        var versionResult = await context.GetModule<GenerateVersionModule>();

        var version = versionResult.ValueOrDefault!;

        var packedProjects = new List<PackedProject>();

        foreach (var project in projects.ValueOrDefault!)
        {
            var packageVersion = BetaPackages.Contains(project.NameWithoutExtension)
                ? $"{version.SemVer!}-beta"
                : version.SemVer!;

            await context.DotNet()
                .Pack(
                    new DotNetPackOptions
                    {
                        ProjectSolution = project.Path,
                        Properties =
                        [
                            new KeyValue("Version", packageVersion),
                            new KeyValue("PackageVersion", packageVersion),
                            new KeyValue("AssemblyFileVersion", version.SemVer!),
                            new KeyValue("IsPackTarget", "true")
                        ],
                        IncludeSource = project == Sourcy.DotNet.Projects.TUnit_Templates ? false : true,
                        Configuration = "Release",
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

            packedProjects.Add(new PackedProject(project.NameWithoutExtension, packageVersion));
        }

        return packedProjects;
    }
}
