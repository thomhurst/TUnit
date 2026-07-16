using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[RunOnlyOnBranch("main")]
[RunOnLinuxOnly]
[DependsOn<GenerateVersionModule>]
public class PushVersionTagModule : Module<CommandResult>
{
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithIgnoreFailuresWhen(async (ctx, ex) =>
        {
            var versionInformation = await ctx.GetModule<GenerateVersionModule>();
            return ex.Message.Contains($"tag 'v{versionInformation.ValueOrDefault!.SemVer}' already exists");
        })
        .Build();

    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionInformation = await context.GetModule<GenerateVersionModule>();

        await context.Git().Commands.Tag(new GitTagOptions
        {
            TagName = $"v{versionInformation.ValueOrDefault!.SemVer}",
        }, token: cancellationToken);

        return await context.Git().Commands.Push(new GitPushOptions
        {
            Tags = true
        }, token: cancellationToken);
    }
}
