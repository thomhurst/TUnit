using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
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
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var versionInformation = await GetModule<GenerateVersionModule>();

        await context.Git().Commands.Tag(new GitTagOptions
        {
            Arguments = [$"v{versionInformation.Value!.SemVer}"],
        }, cancellationToken);

        return await context.Git().Commands.Push(new GitPushOptions
        {
            Tags = true
        }, cancellationToken);
    }
}
