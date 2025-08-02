using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

public class GenerateVersionModule : Module<GitVersionInformation>
{
    protected override async Task<GitVersionInformation?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var versionInformation = await context.Git().Versioning.GetGitVersioningInformation();

        context.LogOnPipelineEnd($"NuGet Version is: {versionInformation.SemVer}");

        return versionInformation;
    }
}
