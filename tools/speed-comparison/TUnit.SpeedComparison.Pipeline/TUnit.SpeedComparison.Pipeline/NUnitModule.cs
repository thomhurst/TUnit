using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.SpeedComparison.Pipeline;

[NotInParallel("SpeedComparison")]
public class NUnitModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.AssertExists().FindFile(x => x.Name == "NUnitTimer.csproj")
            .AssertExists();

        return await context.DotNet().Test(new DotNetTestOptions(project), cancellationToken);
    }
}