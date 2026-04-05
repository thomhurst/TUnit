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
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    // Packages in beta get a "-beta" suffix appended to their version.
    // Remove entries from this set once a package is considered stable.
    private static readonly HashSet<string> BetaPackages =
    [
        "TUnit.Mocks",
        "TUnit.Mocks.Assertions",
        "TUnit.Mocks.Http",
        "TUnit.Mocks.Logging"
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
            var isBeta = BetaPackages.Contains(project.NameWithoutExtension);
            var packageVersion = isBeta
                ? $"{version.SemVer!}-beta"
                : version.SemVer!;

            var properties = new List<KeyValue>
            {
                new KeyValue("PackageVersion", packageVersion),
                new KeyValue("AssemblyVersion", version.AssemblySemVer!),
                new KeyValue("FileVersion", version.AssemblySemFileVer!),
                new KeyValue("InformationalVersion", version.InformationalVersion!),
                new KeyValue("Version", version.SemVer!),
            };

            await context.DotNet()
                .Pack(
                    new DotNetPackOptions
                    {
                        ProjectSolution = project.Path,
                        Properties = properties,
                        IncludeSource = project == Sourcy.DotNet.Projects.TUnit_Templates ? false : true,
                        Configuration = "Release",
                        NoBuild = true,
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
