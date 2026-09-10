using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Git.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Octokit;

namespace TUnit.Pipeline.Modules;

[RunOnLinuxOnly]
[RunOnlyOnBranch("main")]
[DependsOn<UploadToNuGetModule>(Optional = true)]
[DependsOn<GenerateVersionModule>]
public class CreateReleaseModule : Module<Release>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithSkipWhen(async ctx =>
        {
            if (ctx.GetModuleIfRegistered<UploadToNuGetModule>() is not { } uploadToNuGetModule)
            {
                return SkipDecision.Skip("UploadToNuGetModule not registered");
            }

            var result = await uploadToNuGetModule;

            return result.IsSkipped
                ? SkipDecision.Skip("UploadToNuGetModule was skipped")
                : SkipDecision.DoNotSkip;
        })
        .Build();

    protected override async Task<Release?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionModule = await context.GetModule<GenerateVersionModule>();

        var version = versionModule.ValueOrDefault!.SemVer;

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
