using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.PlatformNeutral)]
[NotInParallel("SnapshotTests")]
public class RunPublicAPITestsModule : TestBaseModule
{
    // Public API snapshots cover every supported consumer TFM — keep aligned with
    // <TargetFrameworks> in TUnit.PublicAPI.csproj.
    protected override IEnumerable<string> TestableFrameworks => ModernAndNetFrameworks;

    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.FindFile(x => x.Name == "TUnit.PublicAPI.csproj").AssertExists();

        return (
            new DotNetRunOptions
            {
                NoBuild = true,
                Configuration = "Release",
                Framework = framework,
            },
            new CommandExecutionOptions
            {
                WorkingDirectory = project.Folder!.Path,
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["DISABLE_GITHUB_REPORTER"] = "true",
                    ["GITHUB_ACTIONS"] = "false",
                }
            }
        );
    }
}
