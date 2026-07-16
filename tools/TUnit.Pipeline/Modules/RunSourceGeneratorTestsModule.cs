using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Options;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("SnapshotTests")]
public class RunSourceGeneratorTestsModule : TestBaseModule
{
    // Generator output snapshots verify behaviour across consumer TFMs — keep aligned with
    // <TargetFrameworks> in TUnit.Core.SourceGenerator.Tests.csproj.
    protected override IEnumerable<string> TestableFrameworks
    {
        get
        {
            yield return "net10.0";
            yield return "net8.0";

            if (IsWindows)
            {
                yield return "net472";
            }
        }
    }

    protected override Task<(DotNetRunOptions Options, CommandExecutionOptions? ExecutionOptions)> GetTestOptions(IModuleContext context, string framework, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Core.SourceGenerator.Tests.csproj").AssertExists();

        return Task.FromResult<(DotNetRunOptions, CommandExecutionOptions?)>((
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
        ));
    }
}
