using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.Git;
using Octokit;

namespace TUnit.Pipeline.Modules;

[RunIf<OnLinux>]
[RunOnlyOnBranch("main")]
[DependsOn<UploadToNuGetModule>(Optional = true)]
[DependsOn<GenerateVersionModule>]
public class CreateReleaseModule : Module<Release>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithSkipWhen(async (ctx, cancellationToken) =>
        {
            if (ctx.GetModuleIfRegistered<UploadToNuGetModule>() is not { } uploadToNuGetModule)
            {
                return SkipDecision.Skip("UploadToNuGetModule not registered");
            }

            var result = await uploadToNuGetModule;

            return result is ModuleResult<CommandResult[]>.Skipped
                ? SkipDecision.Skip("UploadToNuGetModule was skipped")
                : SkipDecision.DoNotSkip;
        });

    protected override async Task<Release> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionModule = await context.GetModule<GenerateVersionModule>();

        var version = versionModule.ValueOrDefault!.SemVer;

        var repositoryId = long.Parse(context.Tools.GitHub.EnvironmentVariables.RepositoryId!);

        var lastRelease = await context.Tools.GitHub.Client.Repository.Release.GetLatest(repositoryId);

        var releaseNotes = await context.Tools.GitHub.Client.Repository.Release.GenerateReleaseNotes(repositoryId, new GenerateReleaseNotesRequest($"v{version}")
        {
            PreviousTagName = lastRelease.TagName
        });

        return await context.Tools.GitHub.Client.Repository.Release.Create(
            repositoryId,
            new NewRelease($"v{version}")
            {
                Name = version,
                GenerateReleaseNotes = false,
                Body = releaseNotes.Body,
            });
    }
}
