using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using TUnit.Pipeline.Extensions;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests"), RunOnLinuxOnly, RunOnWindowsOnly]
public class RunAspNetTestsModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Example.Asp.Net.TestProject.csproj").AssertExists();

        return await context.DotNet().RunQuiet(new DotNetRunOptions
        {
            Project = project.Name,
            NoBuild = true,
            Configuration = Configuration.Release,
            Framework = "net9.0",
            WorkingDirectory = project.Folder!,
            Arguments = ["--ignore-exit-code", "8"],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            }
        }, cancellationToken);
    }
}
