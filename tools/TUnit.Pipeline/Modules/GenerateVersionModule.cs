using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.Git;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.Packaging)]
public class GenerateVersionModule : Module<GitVersionInformation>
{
    protected override async Task<GitVersionInformation> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        // In CI the resolve-version action has already chosen the version, from GitVersion or from
        // a published release's tag, and exported it for MSBuild. Packages must carry the same
        // version as the assemblies that action stamped, so reuse it rather than computing another.
        var versionInformation = FromCiEnvironment()
            ?? await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

        context.Logger.LogInformation("NuGet Version is: {SemVer}", versionInformation.SemVer);

        return versionInformation;
    }

    private static GitVersionInformation? FromCiEnvironment()
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
            || Environment.GetEnvironmentVariable("Version") is not { Length: > 0 } version)
        {
            return null;
        }

        return new GitVersionInformation
        {
            SemVer = version,
            FullSemVer = Environment.GetEnvironmentVariable("PackageVersion") ?? version,
            AssemblySemVer = Environment.GetEnvironmentVariable("AssemblyVersion"),
            AssemblySemFileVer = Environment.GetEnvironmentVariable("FileVersion"),
            InformationalVersion = Environment.GetEnvironmentVariable("InformationalVersion"),
        };
    }
}
