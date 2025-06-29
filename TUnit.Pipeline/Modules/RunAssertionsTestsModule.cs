using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

[NotInParallel("DotNetTests")]
public class RunAssertionsTestsModule : TestBaseModule
{
    protected override Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.Assertions.Tests.csproj").AssertExists();

        return Task.FromResult(new DotNetRunOptions
        {
            WorkingDirectory = project.Folder!,
            NoBuild = true,
            Configuration = Configuration.Release,
            Framework = framework,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DISABLE_GITHUB_REPORTER"] = "true",
            }
        });
    }
}
