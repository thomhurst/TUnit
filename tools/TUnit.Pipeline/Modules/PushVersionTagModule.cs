using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.Git;
using ModularPipelines.Git.Options;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunIf<OnLinux>]
[DependsOn<GenerateVersionModule>]
public class PushVersionTagModule : Module<CommandResult>
{
    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithIgnoreFailuresWhen(async (ctx, ex, cancellationToken) =>
        {
            var versionInformation = await ctx.GetModule<GenerateVersionModule>();
            return ex.Message.Contains($"tag 'v{versionInformation.ValueOrDefault!.SemVer}' already exists");
        });

    protected override async Task<CommandResult> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionInformation = await context.GetModule<GenerateVersionModule>();

        await context.Tools.Git.Commands.Branches.TagAsync(new GitTagOptions
        {
            TagName = $"v{versionInformation.ValueOrDefault!.SemVer}",
        }, cancellationToken: cancellationToken);

        return await context.Tools.Git.Commands.Remotes.PushAsync(new GitPushOptions
        {
            Tags = true
        }, cancellationToken: cancellationToken);
    }
}
