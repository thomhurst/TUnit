using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests")]
public class RunPlaywrightTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = Sourcy.DotNet.Projects.TUnit_Templates__content__TUnit_Playwright__TestProject;
        
        return await context.DotNet().Test(new DotNetTestOptions(project.FullName)
        {
            NoBuild = true,
            Configuration = Configuration.Release,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            }
        }, cancellationToken);
    }
}