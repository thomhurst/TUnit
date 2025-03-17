using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests")]
public class RunPublicAPITestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.PublicAPI.csproj").AssertExists();
        
        return await context.DotNet().Run(new DotNetRunOptions
        {
            Project = project,
            NoBuild = true,
            Configuration = Configuration.Release,
            Framework = Environment.GetEnvironmentVariable("NET_VERSION"),
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
                ["GITHUB_ACTIONS"] = "false",
            }
        }, cancellationToken);
    }
}