using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

// Publishes the TESTPROJECT_AOT app that the engine tests' AOT mode runs. As a module it overlaps
// the first test modules instead of delaying the whole pipeline. Version props come from
// $GITHUB_ENV (see .github/workflows/dotnet.yml), so any recompile it triggers stamps the correct
// AssemblyVersion; PackTUnitFilesModule still waits for it (issue #5622).
public class PublishTestProjectAOTModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var rootDir = context.Git().RootDirectory.AssertExists();
        var project = rootDir.FindFile(x => x.Name == "TUnit.TestProject.csproj").AssertExists();

        return await context.DotNet().Publish(new DotNetPublishOptions
        {
            ProjectSolution = project.Path,
            Runtime = RuntimeIdentifiers.Current,
            Configuration = "Release",
            Output = Path.Combine(rootDir.Path, "TESTPROJECT_AOT"),
            Properties =
            [
                new KeyValue("Aot", "true"),
            ],
            Framework = "net10.0",
        }, new CommandExecutionOptions
        {
            LogSettings = new CommandLoggingOptions
            {
                ShowCommandArguments = true,
                ShowStandardError = true,
                ShowExecutionTime = true,
                ShowExitCode = true
            }
        }, cancellationToken);
    }
}
