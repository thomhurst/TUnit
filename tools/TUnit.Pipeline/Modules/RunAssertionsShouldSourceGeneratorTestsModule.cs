using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[ModuleCategory(PipelineCategories.PlatformNeutral)]
[NotInParallel("SnapshotTests")]
public class RunAssertionsShouldSourceGeneratorTestsModule : TestBaseModule
{
    // Generator output snapshots verify behaviour across consumer TFMs — keep aligned with
    // <TargetFrameworks> in TUnit.Assertions.Should.SourceGenerator.Tests.csproj. net472 is
    // intentionally omitted there: the generator output is consumer-TFM-independent and the
    // .NET Framework BCL trips the in-memory compilation harness for user-declared generics.
    protected override IEnumerable<string> TestableFrameworks => ModernFrameworks;

    protected override async Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = (await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken)).Root.FindFile(x => x.Name == "TUnit.Assertions.Should.SourceGenerator.Tests.csproj").AssertExists();

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
                }
            }
        );
    }
}
