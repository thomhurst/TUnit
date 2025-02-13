using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Git.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Octokit;

namespace TUnit.Pipeline.Modules;

[RunOnLinuxOnly]
[RunOnlyOnBranch("main")]
[DependsOn<UploadToNuGetModule>(IgnoreIfNotRegistered = true)]
[DependsOn<GenerateVersionModule>]
public class CreateReleaseModule : Module<Release>
{
    protected override async Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        if (GetModuleIfRegistered<UploadToNuGetModule>() is not { } uploadToNuGetModule)
        {
            return true;
        }

        var result = await uploadToNuGetModule;

        return result.SkipDecision.ShouldSkip;
    }

    protected override async Task<Release?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var versionModule = await GetModule<GenerateVersionModule>();

        var version = versionModule.Value!.SemVer;

        return await context.GitHub().Client.Repository.Release.Create(
            long.Parse(context.GitHub().EnvironmentVariables.RepositoryId!),
            new NewRelease($"v{version}")
            {
                Name = version,
                GenerateReleaseNotes = true,
            });
    }
}