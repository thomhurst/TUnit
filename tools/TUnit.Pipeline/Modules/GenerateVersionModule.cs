using Microsoft.Extensions.Logging;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

public class GenerateVersionModule : Module<GitVersionInformation>
{
    protected override async Task<GitVersionInformation?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versionInformation = await context.Git().Versioning.GetGitVersioningInformation();

        context.Logger.LogInformation("NuGet Version is: {SemVer}", versionInformation.SemVer);

        return versionInformation;
    }
}
