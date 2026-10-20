using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using ModularPipelines.Options;

namespace TUnit.Pipeline.Modules;

// Publishes the TESTPROJECT_AOT app that the engine tests' AOT mode runs. It runs in aot.yml, on
// runners of its own: inside the test matrix it competed with the test modules for CPUs and was
// the critical path on every OS.
// The publish builds into its own artifacts tree. The global Aot property flows to every
// referenced project, so publishing into the shared bin/obj folders recompiled the TUnit.*
// libraries in place (packing had to wait for it, issue #5622) and rewrote
// TUnit.TestProject/obj/project.assets.json under the RPC tests (NETSDK1047). Isolated, neither
// has to wait for this module.
[ModuleCategory(PipelineCategories.Aot)]
public class PublishTestProjectAOTModule : Module<CommandResult>
{
    // The publish once took 24-27 minutes on a 3-core macOS runner, past the default 30-minute
    // module timeout on healthy runs.
    protected override ModuleConfiguration Configure() => ModuleConfiguration.Create()
        .WithTimeout(TimeSpan.FromMinutes(60))
        .Build();

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
                new KeyValue("ArtifactsPath", Path.Combine(rootDir.Path, "artifacts", "aot-publish")),
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
