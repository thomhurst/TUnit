using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
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

        // Rebuild all library projects with the pack version to ensure consistent assembly versions.
        // Without this, project references may retain stale MinVer-computed versions from the
        // initial 'dotnet build' step, causing CS1705 assembly version mismatches on PR branches.
        await context.DotNet()
            .Build(
                new DotNetBuildOptions
                {
                    ProjectSolution = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.slnx").AssertExists().Path,
                    Properties =
                    [
                        new KeyValue("Version", version.SemVer!),
                        new KeyValue("AssemblyFileVersion", version.SemVer!),
                        new KeyValue("IsPackTarget", "true")
                    ],
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
