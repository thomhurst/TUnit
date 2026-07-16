using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("NetworkTests")]
[DependsOn<InstallPlaywrightModule>]
public class RunPlaywrightTestsModule : TestBaseModule
{
    protected override Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = Path.Combine(
            context.Git().RootDirectory.Path,
            "src",
            "TUnit.Templates",
            "content",
            "TUnit.Playwright",
            "TestProject.csproj");

        return Task.FromResult<(DotNetRunOptions, CommandExecutionOptions?)>((
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
                }
            }
        ));
    }
}
