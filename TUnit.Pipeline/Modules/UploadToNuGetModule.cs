using EnumerableAsyncProcessor.Extensions;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[SkipIfBranch("main")]
[DependsOn<PackTUnitFilesModule>]
public class UploadToNuGetModule : Module<CommandResult[]>
{
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var nupkgs = context.Git().RootDirectory
            .GetFiles(x => x.Extension is ".nupkg" or ".snupkg");

        return await nupkgs.SelectAsync(file =>
                context.DotNet().Nuget.Push(new DotNetNugetPushOptions(file)
                {
                    Source = "https://api.nuget.org/v3/index.json"
                }, cancellationToken), cancellationToken: cancellationToken)
            .ProcessOneAtATime();
    }
}