using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests"), RunOnLinuxOnly, RunOnWindowsOnly]
public class RunPlaywrightTestsModule : TestBaseModule
{
    protected override Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = Sourcy.DotNet.Projects.TUnit_Templates__content__TUnit_Playwright__TestProject;

        return Task.FromResult<(DotNetRunOptions, CommandExecutionOptions?)>((
            new DotNetRunOptions
            {
                Project = project.FullName,
                NoBuild = true,
                Configuration = "Release",
            },
            new CommandExecutionOptions
            {
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["DISABLE_GITHUB_REPORTER"] = "true",
                }
            }
        ));
    }
}
