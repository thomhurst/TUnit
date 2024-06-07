using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules.Tests;

public class BuildTestProjectModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context,
        CancellationToken cancellationToken)
    {
        var projects = context.Git().RootDirectory.GetFiles(x =>
            x.Extension == ".csproj" && !x.Path.Contains("Pipeline", StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            await context.DotNet().Build(new DotNetBuildOptions(project), cancellationToken);
        }

        return null!;
    }
}