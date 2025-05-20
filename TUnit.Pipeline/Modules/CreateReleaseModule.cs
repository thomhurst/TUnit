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
        
        var repositoryId = long.Parse(context.GitHub().EnvironmentVariables.RepositoryId!);

        var lastRelease = await context.GitHub().Client.Repository.Release.GetLatest(repositoryId);

        var releaseNotes = await context.GitHub().Client.Repository.Release.GenerateReleaseNotes(repositoryId, new GenerateReleaseNotesRequest($"v{version}")
        {
            PreviousTagName = lastRelease.TagName
        });

        return await context.GitHub().Client.Repository.Release.Create(
            repositoryId,
            new NewRelease($"v{version}")
            {
                Name = version,
                GenerateReleaseNotes = false,
                Body = releaseNotes.Body,
            });
    }
}