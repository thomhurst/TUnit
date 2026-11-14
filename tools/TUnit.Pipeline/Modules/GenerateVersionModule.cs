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
        var versionInformation = await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

        context.Logger.LogInformation("NuGet Version is: {SemVer}", versionInformation.SemVer);

        return versionInformation;
    }
}
