using EnumerableAsyncProcessor.Extensions;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Attributes;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

// TODO: Re-add
// [RunOnlyOnBranch("main")]
[DependsOn<PackTUnitFilesModule>]
public class UploadToNuGetModule : Module<CommandResult[]>
{
    private readonly IOptions<NuGetOptions> _options;

    protected override async Task<SkipDecision> ShouldSkip(IPipelineContext context)
    {
        await Task.CompletedTask;
        return string.IsNullOrEmpty(_options.Value.ApiKey);
    }

    public UploadToNuGetModule(IOptions<NuGetOptions> options)
    {
        _options = options;
    }
    protected override async Task<CommandResult[]?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var nupkgs = context.Git().RootDirectory
            .GetFiles(x => x.Extension is ".nupkg");

        return await nupkgs.SelectAsync(file =>
                context.DotNet().Nuget.Push(new DotNetNugetPushOptions(file)
                {
                    Source = "https://api.nuget.org/v3/index.json",
                    ApiKey = _options.Value.ApiKey
                }, cancellationToken), cancellationToken: cancellationToken)
            .ProcessOneAtATime();
    }
}