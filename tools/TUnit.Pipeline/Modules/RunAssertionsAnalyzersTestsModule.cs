using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.PlatformNeutral)]
public class RunAssertionsAnalyzersTestsModule : TestBaseModule
{
    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.FindFile(x => x.Name == "TUnit.Assertions.Analyzers.Tests.csproj").AssertExists();

        return (
            new DotNetRunOptions
            {
                NoBuild = true,
                Configuration = "Release",
                Framework = framework,
                Arguments = [.. HangDumpArguments.Create($"hangdump.assertions-analyzers-tests-{framework}.dmp", "5m")],
            },
            new CommandExecutionOptions
            {
                WorkingDirectory = project.Folder!.Path,
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    ["DISABLE_GITHUB_REPORTER"] = "true",
                }
            }
        );
    }
}
