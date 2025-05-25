using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

public class RunUnitTestsModule : TestBaseModule
{
    protected override Task<DotNetRunOptions> GetTestOptions(IPipelineContext context, string framework, CancellationToken cancellationToken)
    {
        var project = context.Git().RootDirectory.FindFile(x => x.Name == "TUnit.UnitTests.csproj").AssertExists();
        
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