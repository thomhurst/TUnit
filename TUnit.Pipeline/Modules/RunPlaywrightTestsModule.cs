using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests"), RunOnLinuxOnly, RunOnWindowsOnly]
public class RunPlaywrightTestsModule : TestBaseModule
{
    protected override Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework, CancellationToken cancellationToken)
    {
        var project = Sourcy.DotNet.Projects.TUnit_Templates__content__TUnit_Playwright__TestProject;

        return Task.FromResult(new DotNetRunOptions
        {
            Project = project.FullName,
            NoBuild = true,
            Configuration = Configuration.Release,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            }
        });
    }
}
