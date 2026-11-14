using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("NetworkTests")]
[DependsOn<InstallPlaywrightModule>]
public class RunPlaywrightTestsModule : TestBaseModule
{
    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = Path.Combine(
            (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.Path,
            "src",
            "TUnit.Templates",
            "content",
            "TUnit.Playwright",
            "TestProject.csproj");

        return (
            new DotNetRunOptions
            {
                Project = project,
                NoBuild = true,
                Configuration = "Release",
            },
            new CommandExecutionOptions
            {
                WorkingDirectory = Path.GetDirectoryName(project),
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["DISABLE_GITHUB_REPORTER"] = "true",
                    ["TUNIT_DISABLE_HTML_REPORTER"] = "true",
                }
            }
        );
    }
}
