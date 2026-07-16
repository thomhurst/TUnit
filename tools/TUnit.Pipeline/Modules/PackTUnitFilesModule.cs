using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

// Must run after any module that may recompile packable projects (TUnit.Mocks et al).
// This module uses NoBuild=true and packs whatever bin/Release/{tfm}/*.dll is on disk, so
// a racing recompile without version props would ship strong-name-mismatched packages.
// Version props come from $GITHUB_ENV (see .github/workflows/dotnet.yml) so recompiles
// also stamp the correct AssemblyVersion. See issue #5622.
[DependsOn<GetPackageProjectsModule>]
[DependsOn<GenerateVersionModule>]
[DependsOn<PublishMockTestsAOTModule>]
public class PackTUnitFilesModule : Module<List<PackedProject>>
{
    // Packages in beta get a "-beta" suffix appended to their version.
    // Remove entries from this set once a package is considered stable.
    private static readonly HashSet<string> BetaPackages =
    [
        "TUnit.Assertions.Should"
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
            var projectName = project.NameWithoutExtension;
            var isBeta = BetaPackages.Contains(projectName);
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
                        IncludeSource = projectName != "TUnit.Templates",
                        Configuration = "Release",
                        // The reporting tool is a standalone dotnet tool no test module
                        // references, so nothing upstream has restored or built it — let
                        // pack build it (the strong-name race in the header comment doesn't
                        // apply: it isn't strong-named and nothing else consumes its bits).
                        NoBuild = projectName != "TUnit.Reporting.Tool",
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

            packedProjects.Add(new PackedProject(projectName, packageVersion));
        }

        return packedProjects;
    }
}
